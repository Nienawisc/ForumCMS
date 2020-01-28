using ForumCMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ForumCMS.Controllers
{
    public class HomeController : Controller
    {
        private ForumGameEntities db = new ForumGameEntities();
        public ActionResult Index()
        {
            return View(db.Kategoria.ToList());
        }
        public ActionResult Logowanie()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(string login, string haslo, string autologin)
        {
            var user = db.zaloguj(login, haslo).FirstOrDefault();
            if (user == null)
            {
                ViewBag.error = "Błędny login lub hasło";
                return View("Login");
            }
            var finded = db.User.Find(user.id);
            if (finded == null)
            {
                ViewBag.error = "Błędny login lub hasło";
                return View("Login");
            }
            if (autologin == "on")
            {
                HttpCookie ciasteczko = new HttpCookie("autologin");
                ciasteczko.Values.Add("userId", finded.id.ToString());
                ciasteczko.Values.Add("autokod", finded.autokod.ToString());

                //czas wygasniecia:
                ciasteczko.Expires = DateTime.Now.AddDays(30);
                Response.Cookies.Add(ciasteczko);
            }
            finded.lastLogin = DateTime.Now;
            var uu = db.User.Find(finded.id);
            Session["User"] = uu.id;
            Session["UserLogin"] = uu.login;
            db.Entry(finded).State = EntityState.Modified;
            db.SaveChanges();
        
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public ActionResult Rejestracja(string login, string pass, string pass2, string email)
        {
            if ((db.User.Where(x => x.login == login).Count()) > 0)
            {
                return Json(new { success = false, message = "Podany login istnieje w bazie danych proszę podać inny" }, JsonRequestBehavior.AllowGet);
            }
            if ((db.User.Where(x => x.email == email).Count()) > 0)
            {
                return Json(new { success = false, message = "Podany email istnieje w bazie danych proszę podać inny" }, JsonRequestBehavior.AllowGet);
            }

            if (pass == pass2)
            {
                var u = db.rejestruj(login, pass, email).FirstOrDefault();
                var uu = db.User.Find(u.id);
                uu.lastLogin = DateTime.Now;
                db.SaveChanges();
                Session["User"] = u.id;
                Session["UserLogin"] = uu.login;
                return RedirectToAction("Index");
            }
            else
            {
                return Json(new { success = false, message = "Podane hasła są niezgodne" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Wyloguj()
        {
            HttpCookie ciasteczko = Request.Cookies["autologin"];
            if (ciasteczko != null)
            {
                ciasteczko.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(ciasteczko);
            }
            Session.Abandon();
            HttpContext.Request.Cookies.Clear();
            return RedirectToAction("Index");
        }
    }
}