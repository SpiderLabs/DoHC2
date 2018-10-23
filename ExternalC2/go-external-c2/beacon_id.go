package externc2

import (
	"fmt"

	"github.com/google/uuid"
)

// BeaconID is  combination of the internal identifier and Cobalt Strike identifier.
type BeaconID struct {
	CobaltStrikeID int
	InternalID     string
}

// ToString returns this as a string.
func (b *BeaconID) ToString() string {
	return fmt.Sprintf("%d_%s", b.CobaltStrikeID, b.InternalID)
}

// NewBeaconID returns a new BeaconID with the InternalID initialized.
func NewBeaconID() (BeaconID, error) {
	var beaconID BeaconID
	u, err := uuid.NewRandom()
	if err != nil {
		return beaconID, err
	}
	beaconID.InternalID = u.String()
	return beaconID, nil
}
