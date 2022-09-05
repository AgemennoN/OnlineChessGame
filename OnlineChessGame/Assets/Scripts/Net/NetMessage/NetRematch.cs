using Unity.Networking.Transport;
using UnityEngine;

public class NetRematch : NetMessage
{
    public int teamId;
    public byte wantRematch;

    public NetRematch() // Making the Box
    {
        Code = OpCode.REMATCH;
    }
    public NetRematch(DataStreamReader reader) // Receiving the Box
    {
        Code = OpCode.REMATCH;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(teamId);
        writer.WriteByte(wantRematch);
    }
    public override void Deserialize(DataStreamReader reader)
    {
        // We already read the byte in NetUtility::OnData 
        teamId = reader.ReadInt();
        wantRematch = reader.ReadByte();
    }

    public override void ReveivedOnClient()
    {
        NetUtility.C_REMATCH?.Invoke(this);
    }
    public override void ReveivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_REMATCH?.Invoke(this, cnn);
    }


}
