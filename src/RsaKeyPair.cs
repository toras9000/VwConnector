using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VwConnector;

public record RsaKeyPair(byte[] PublicKey, byte[] PrivateKey);