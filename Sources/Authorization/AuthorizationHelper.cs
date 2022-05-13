
namespace YITC.Proxy.Authorization
{
    public static class AuthorizationHelper
    {
        public static bool HasBetriebenummer(int betriebenummer)
        {
            if (betriebenummer != 0)
                return true;

            return false;
        }
    }
}
