using System.Runtime.InteropServices;

public class UdpPacket {
    public enum PacketType : byte {
        Ping = 0x01,
        Pong = 0x02,
        Joystick = 0x03
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader {
        // On met le CRC en premier pour une validation immédiate
        public uint Crc32;          // 4 octets
        public ushort TotalLength;  // 2 octets
        public PacketType PacketType;     // 1 octet
        public uint UserId;         // 4 octets
        public uint SequenceId;     // 4 octets
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerInputPayload {
        public float X; // 4 octets
        public float Y; // 4 octets
    }
}