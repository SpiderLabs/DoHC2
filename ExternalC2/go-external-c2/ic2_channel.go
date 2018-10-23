package externc2

// IC2Channel is used to communicate with the beacon
// and server protocols.
type IC2Channel interface {
	Connect() error
	IsConnected() bool
	Close()
	ReadFrame() ([]byte, int, error)
	SendFrame(buffer []byte) (int, error)
	ReadAndSendTo(c2 IC2Channel)
	GetStager(pipeName string, is64Bit bool, taskWaitTime int) ([]byte, error)
}
