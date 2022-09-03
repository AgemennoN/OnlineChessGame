using Unity.Networking.Transport;

public class NetStartGame : NetMessage
{
    public NetStartGame() // Making the Box
    {
        Code = OpCode.START_GAME;
    }
    public NetStartGame(DataStreamReader reader) // Receiving the Box
    {
        Code = OpCode.START_GAME;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
    }
    public override void Deserialize(DataStreamReader reader)
    {

    }

    public override void ReveivedOnClient()
    {
        NetUtility.C_START_GAME?.Invoke(this);
    }
    public override void ReveivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_START_GAME?.Invoke(this, cnn);
    }


}
