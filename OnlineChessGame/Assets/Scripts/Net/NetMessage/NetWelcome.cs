using Unity.Networking.Transport;
using UnityEngine;

public class NetWelcome : NetMessage
{
    public int AssignedTeam { get; set; }
    public NetWelcome() // Making the Box
    {
        Debug.Log("New Welcome created");
        Code = OpCode.WELCOME;
    }
    public NetWelcome(DataStreamReader reader) // Receiving the Box
    {
        Debug.Log("New Welcome recieved");
        Code = OpCode.WELCOME;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(AssignedTeam);
    }
    public override void Deserialize(DataStreamReader reader)
    {
        // We already read the byte in NetUtility::OnData 
        AssignedTeam = reader.ReadInt();
    }

    public override void ReveivedOnClient()
    {
        NetUtility.C_WELCOME?.Invoke(this);
    }
    public override void ReveivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_WELCOME?.Invoke(this, cnn);
    }


}
