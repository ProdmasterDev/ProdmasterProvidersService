namespace ProdmasterProvidersService.Extensions
{
    public static class DebugModeDeterminant
    {
        public static bool IsDebug(this IWebHostEnvironment environment)
        {
            return IsDebug();
        }

        private static bool IsDebug()
        {
            #if DEBUG
                return true;
            #else
                return false;
            #endif
        }
    }
}
