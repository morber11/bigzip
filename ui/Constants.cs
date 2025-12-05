namespace BigZipUI
{
    public static class Constants
    {
        public const string BIGZIP_EXTENSION = ".bigzip";
        public const string CLI_EXECUTABLE_NAME = "bz.exe";

        public const double PROGRESS_INIT = 0.1;
        public const double PROGRESS_ARGS_READY = 0.2;
        public const double PROGRESS_PROCESS_STARTED = 0.3;
        public const double PROGRESS_INCREMENT = 0.02;
        public const double PROGRESS_MAX_INCREMENTAL = 0.95;
        public const double PROGRESS_COMPLETE = 1.0;
        public const int PROGRESS_UPDATE_DELAY_MS = 200;
    }
}