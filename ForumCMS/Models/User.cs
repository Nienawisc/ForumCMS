//------------------------------------------------------------------------------
// <auto-generated>
//    Ten kod źródłowy został wygenerowany na podstawie szablonu.
//
//    Ręczne modyfikacje tego pliku mogą spowodować nieoczekiwane zachowanie aplikacji.
//    Ręczne modyfikacje tego pliku zostaną zastąpione w przypadku ponownego wygenerowania kodu.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ForumCMS.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class User
    {
        public User()
        {
            this.Post = new HashSet<Post>();
            this.Temat = new HashSet<Temat>();
        }
    
        public int id { get; set; }
        public string login { get; set; }
        public string pass { get; set; }
        public string autokod { get; set; }
        public Nullable<System.DateTime> lastLogin { get; set; }
        public string email { get; set; }
        public bool admin { get; set; }
    
        public virtual ICollection<Post> Post { get; set; }
        public virtual ICollection<Temat> Temat { get; set; }
    }
}
