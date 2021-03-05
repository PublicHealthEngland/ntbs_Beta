﻿namespace ntbs_service.Properties
{
    public class AdOptions
    {
        public string NationalTeamAdGroup { get; set; }

        /** TB service groups are of the format Global.NIS.NTBS.Service_<service-specifc-postfix> */
        public string ServiceGroupAdPrefix { get; set; }

        /** Group that contains all users of NTBS, used for authorizing access to the app */
        public string BaseUserGroup { get; set; }

        /** Group that contains admin users only, used for authorizing access protected pages of the app */
        public string AdminUserGroup { get; set; }

        /** Only used in development mode, allows developers to set their group membership via properties */
        public string DevGroup { get; set; }

        /** Only used in the CI environment to bypass authentication when running UI tests */
        public bool UseDummyAuth { get; set; }
    }
}