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
    
    public partial class Kategoria
    {
        public Kategoria()
        {
            this.Temat = new HashSet<Temat>();
        }
    
        public int id { get; set; }
        public string nazwa { get; set; }
        public string opis { get; set; }
        public bool aktywna { get; set; }
        public Nullable<int> kolejnosc { get; set; }
        public bool tylko_dla_zalogowanych { get; set; }
    
        public virtual ICollection<Temat> Temat { get; set; }
    }
}
