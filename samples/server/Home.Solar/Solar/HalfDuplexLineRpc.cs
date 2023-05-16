using System;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Solar
{
    class HalfDuplexLineRpc : BaseRpc
    {
        public enum Error
        {
            Ok,
            Overflow,
            FrameError,

            // ClientError
            ClientNoData = 32
        }

        public async Task<Tuple<byte[], Error>> SendReceive(byte[] txData, bool wantsResponse, bool echo, string opName)
        {
            throw new NotImplementedException();
        }
    }
}
