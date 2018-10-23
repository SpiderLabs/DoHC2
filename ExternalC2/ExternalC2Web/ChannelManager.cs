using System;
using System.Collections.Concurrent;
using System.Linq;
using ExternalC2;

namespace ExternalC2Web
{
    /// <summary>
    ///     Manages the relationships between beacons and socket connections
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChannelManager<T> where T : class, IDisposable
    {
        private readonly ConcurrentDictionary<BeaconId, T> _channels =
            new ConcurrentDictionary<BeaconId, T>();

        /// <summary>
        ///     Gets all channels
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<BeaconId, T> GetAll()
        {
            return _channels;
        }

        /// <summary>
        ///     Gets the channel by Beacon identifier
        /// </summary>
        /// <param name="id">The Beacon identifier.</param>
        /// <returns></returns>
        public T GetChannelById(BeaconId id)
        {
            return _channels.FirstOrDefault(p => p.Key.ToString() == id.ToString()).Value;
        }

        /// <summary>
        ///     Gets the identifier for the channel
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns></returns>
        public BeaconId GetId(T channel)
        {
            return _channels.FirstOrDefault(p => p.Value == channel).Key;
        }

        /// <summary>
        ///     Adds the channel.
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="channel">The channel</param>
        /// <returns></returns>
        public BeaconId AddChannel(BeaconId id, T channel)
        {
            _channels.TryAdd(id, channel);
            return GetId(channel);
        }

        /// <summary>
        ///     Removes the channel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public void RemoveChannel(BeaconId id)
        {
            _channels.TryRemove(id, out var socket);
            socket.Dispose();
        }
    }
}