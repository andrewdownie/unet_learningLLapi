
/// <summary>
/// This class is to be used as a central way of transforming data between local and network forms.
/// Regardless of how trivial the transformation is, transformations between local and network forms
/// should go here.
/// </summary>
public static class NetworkConverter {

	public static byte[] StrToNet(string str)
    {
        return System.Text.Encoding.UTF8.GetBytes(str);
    }

    public static string NetToStr(byte[] net, int netSize)
    {
        return System.Text.Encoding.UTF8.GetString(net, 0, netSize);
    }

    

    
}
 