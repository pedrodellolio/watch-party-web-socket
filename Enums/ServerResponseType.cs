using System.ComponentModel.DataAnnotations;

namespace WatchParty.WS.Enums
{
    public enum ServerResponseType
    {
        [Display(Name = "COMMAND")]
        COMMAND,
        [Display(Name = "MESSAGE")]
        MESSAGE,
        [Display(Name = "SYSTEM")]
        SYSTEM
    }
}
