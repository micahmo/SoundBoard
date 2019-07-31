#region Usings

using NAudio.Wave;

#endregion

namespace SoundBoard
{
    #region LoopStream class

    /// <summary>
    /// Stream for looping playback.
    /// </summary>
    /// <remarks>
    /// This is a special audio class derived from <see cref="WaveStream"/> which supports looping audio.
    /// Taken from NAudio developer Mark Heath's website: https://markheath.net/post/looped-playback-in-net-with-naudio
    /// </remarks>
    public class LoopStream : WaveStream
    {
        #region Private fields

        readonly WaveStream _sourceStream;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new Loop stream
        /// </summary>
        /// <param name="sourceStream">The stream to read from. Note: the Read method of this stream should return 0 when it reaches the end
        /// or else we will not loop to the start again.</param>
        public LoopStream(WaveStream sourceStream)
        {
            _sourceStream = sourceStream;
            EnableLooping = true;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Use this to turn looping on or off
        /// </summary>
        public bool EnableLooping { get; set; }

        #endregion

        #region Overrides

        /// <summary>
        /// Return source stream's wave format
        /// </summary>
        public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

        /// <summary>
        /// LoopStream simply returns
        /// </summary>
        public override long Length => _sourceStream.Length;

        /// <summary>
        /// LoopStream simply passes on positioning to source stream
        /// </summary>
        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    if (_sourceStream.Position == 0 || !EnableLooping)
                    {
                        // something wrong with the source stream
                        break;
                    }

                    // loop
                    _sourceStream.Position = 0;
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        #endregion
    }

    #endregion
}
