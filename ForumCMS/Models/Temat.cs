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
    
    public partial class Temat
    {
        public Temat()
        {
            this.Post = new HashSet<Post>();
        }
    
        public int id { get; set; }
        public int idK { get; set; }
        public string nazwa { get; set; }
        public Nullable<System.DateTime> czas { get; set; }
        public Nullable<int> idAutora { get; set; }
        public int status { get; set; }
        public Nullable<int> odslony { get; set; }
    
        public virtual Kategoria Kategoria { get; set; }
        public virtual ICollection<Post> Post { get; set; }
        public virtual Status Status1 { get; set; }
        public virtual User User { get; set; }
    }
}
