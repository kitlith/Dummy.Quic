namespace Dummy.Quic;

public static class QuicLog {
    public static Action<String>? Info;
    public static Action<String>? Warn;
    public static Action<String>? Error;
}