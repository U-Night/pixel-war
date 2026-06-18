using System;
using System.Buffers.Binary;
using System.Text;
using Godot;


/// <summary>
/// Types de paquets supportés par le protocole
/// </summary>
public enum PacketType : byte {
	// ═══ Système ═══
	Ping = 0x01,
	Pong = 0x02,
	Handshake = 0x03,

	PlayerJoin = 0x04,
	PlayerLeave = 0x05,

	Message = 0x06,
	Joystick = 0x07,
	
	Disconnect = 0x08,
}

/// <summary>
/// Représente un paquet de données avec son type et son payload
/// 
/// Format binaire :
/// ┌──────────────┬──────────────┬──────────────┐
/// │     TYPE     │  DATA_SIZE   │     DATA     │
/// │    1 byte    │   4 bytes    │   M bytes    │
/// └──────────────┴──────────────┴──────────────┘
/// Ici, c'est la couche OSI "Applicative", on gère la normalisation de notre propre protocole
/// </summary>
public class Packet {
	public PacketType Type { get; }
	public byte[] Data { get; }

	public Packet(PacketType type, byte[] data) {
		Type = type;
		Data = data;
	}

	/// <summary>
	/// Crée un paquet avec des données textuelles
	/// </summary>
	public Packet(PacketType type, string text) : this(type, Encoding.UTF8.GetBytes(text)) { }

	/// <summary>
	/// Crée un paquet sans données
	/// </summary>
	public Packet(PacketType type) : this(type, Array.Empty<byte>()) { }

	/// <summary>
	/// Sérialise le paquet en bytes
	/// </summary>
	public byte[] Serialize() {
		// Calcul de la taille totale
		// [TYPE (1)] + [DATA_SIZE (4)] + [DATA (M)]
		int totalSize = 1 + 4 + Data.Length;
		byte[] buffer = new byte[totalSize];
		int offset = 0;

		// TYPE (1 byte)
		buffer[offset] = (byte)Type;
		offset += 1;

		// DATA_SIZE (4 bytes, int)
		BitConverter.GetBytes(Data.Length).CopyTo(buffer, offset);
		offset += 4;

		// DATA (M bytes)
		Data.CopyTo(buffer, offset);

		return buffer;
	}

	/// <summary>
	/// Désérialise un paquet depuis des bytes
	/// </summary>
	public static Packet Deserialize(byte[] buffer) {
		// 1. Vérification de la taille minimale 
		// [TYPE (1)] + [DATA_SIZE (4)] = 5 octets minimum (même si DATA est vide)
		if (buffer == null || buffer.Length < 5)
			throw new ProtocolViolationException("Paquet trop court (minimum 5 octets attendus).");

		int offset = 0;

		// 2. TYPE (1 byte)
		// On lit directement le premier octet et on le cast dans l'enum
		PacketType type = (PacketType)buffer[offset];
    
		// Sécurité : on vérifie que l'octet reçu correspond bien à une valeur valide de notre Enum
		if (!Enum.IsDefined(typeof(PacketType), type))
			throw new ProtocolViolationException($"Type de paquet inconnu ou corrompu : {buffer[offset]}");
       
		offset += 1;

		// 3. DATA_SIZE (4 bytes, int)
		int dataSize = BitConverter.ToInt32(buffer, offset);
		offset += 4;

		// Sécurité : on s'assure que la taille n'est pas négative et qu'elle ne dépasse pas le buffer
		if (dataSize < 0 || buffer.Length < offset + dataSize)
			throw new ProtocolViolationException($"Paquet malformé : données tronquées. Attendu: {dataSize}, Disponible: {buffer.Length - offset}");

		// 4. DATA (M bytes)
		byte[] data = new byte[dataSize];
    
		// Petite optimisation : on ne copie que s'il y a réellement des données
		if (dataSize > 0) {
			Array.Copy(buffer, offset, data, 0, dataSize);
		}

		return new Packet(type, data);
	}

	/// <summary>
	/// Lit le payload comme du texte UTF-8
	/// </summary>
	public string GetDataAsString() => Encoding.UTF8.GetString(Data);

	public override string ToString() => $"Packet({Type}, {Data.Length} bytes)";
}
