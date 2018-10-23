using System;

namespace ExternalC2
{
    /// <summary>
    ///     A combination of the internal identifier and Cobalt Strike identifier
    /// </summary>
    public struct BeaconId
    {
        /// <summary>
        ///     The cobalt strike identifier
        /// </summary>
        public int CobaltStrikeId;

        /// <summary>
        ///     The internal identifier
        /// </summary>
        public Guid InternalId;

        /// <summary>
        ///     Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
            => $"{CobaltStrikeId}_{InternalId}";
    }
}
