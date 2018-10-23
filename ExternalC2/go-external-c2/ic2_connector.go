package externc2

// IC2Connector is used to connect two IC2Channels.
type IC2Connector interface {
	Started() bool
	BeaconChannel() IC2Channel
	ServerChannel() IC2Channel
	Initialize() bool
	Go()
	Stop()
}
