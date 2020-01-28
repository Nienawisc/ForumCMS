using ForumCMS.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace ForumCMS.Controllers
{
    public class HomeController : Controller
    {
        private ForumGameEntities1 db = new ForumGameEntities1();
        private int paginationLimit = 10;

        public int PaginationLimit { get => paginationLimit; set => paginationLimit = value; }
        public ActionResult Index()
        {
            return View(db.Kategoria.ToList());
        }
        public ActionResult Tematy(int id, string search=null, int page=1)
        {
            if (!czyIstniejeKategoria(id)) return RedirectToAction("Index");

            ViewBag.pagination = paginationLimit;

            decimal maxPage = Math.Ceiling((decimal)db.Temat.Where(t => t.status != 1 && t.idK == id).Count() / (decimal)paginationLimit);

            if (page > maxPage) page = (int)maxPage;
            if (page <= 0) page = 1;

            ViewBag.Kategoria = db.Kategoria.Find(id).nazwa;

            if (search != null)
            {
                return View(db.Temat.ToList().Where(t => t.status != 1 && t.idK == id && t.nazwa.ToLower().Contains(search.ToLower())).ToList()
                    .OrderByDescending(x => x.status).ThenByDescending(y => y.Post.LastOrDefault().czas)
                    .Skip((page - 1) * PaginationLimit).Take(PaginationLimit).ToList());
            }
            return View(db.Temat.ToList().Where(t => t.status != 1 && t.idK == id).ToList()
                .OrderByDescending(x => x.status).ThenByDescending(y => y.Post.LastOrDefault().czas)
                .Skip((page - 1) * PaginationLimit).Take(PaginationLimit).ToList());
        }
        private bool czyZalogowany()
        {
            if (Session["User"] != null) return true;
            return false;
        }
        private bool czyIstniejeKategoria(int? id)
        {
            if (id == null) return false;
            var kategoria = db.Kategoria.Find(id);
            if (kategoria == null) return false;
            if (!(bool)kategoria.aktywna && Session["Admin"] == null) return false;
            if ((bool)kategoria.tylko_dla_zalogowanych && Session["UserId"] == null) return false;
            return true;
        }
        // GET: forum_tematy/Create
        public ActionResult UtworzTemat(int? id)
        {
            if (!czyZalogowany()) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            //sprawdzenie czy istnieje taka kategoria
            if (!czyIstniejeKategoria(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ViewBag.Kategoria = db.Kategoria.Find(id).nazwa;
            return View(db.Kategoria.Find(id).Temat.FirstOrDefault());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult UtworzTemat(string naglowek, string tresc, int id)
        {
            //sprawdzenie czy istnieje taka kategoria
            if (!czyZalogowany()) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!czyIstniejeKategoria(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Temat temat = new Temat();
            temat.nazwa = naglowek;
            temat.czas = DateTime.Now;
            temat.idAutora = (int)Session["User"];
            temat.status = 2;
            temat.idK = id;
            temat.odslony = 0;
            if (ModelState.IsValid)
            {
                db.Temat.Add(temat);
                db.SaveChanges();
                Post post = new Post();
                post.tresc = tresc;
                post.idAutora = (int)temat.idAutora;
                post.czas = temat.czas;
                post.status = 2;
                post.idT = temat.id;

                db.Post.Add(post);
                db.SaveChanges();

                ViewBag.Kategoria = temat.Kategoria.nazwa;
                return RedirectToAction("TematSzczegoly", new { id = temat.id });
            }

            return View();
        }
        private bool czyIstniejeTemat(int? id)
        {
            if (id == null) return false;
            var temat = db.Temat.Find(id);

            if (temat == null) return false;
            if ((bool)temat.Kategoria.tylko_dla_zalogowanych && Session["User"] == null) return false;
            if (temat.status == 1) return false;
            return true;
        }
        public ActionResult TematSzczegoly(int? id, int page = 1)
        {
            if (!czyIstniejeTemat(id)) return RedirectToAction("Index");
            var forum_posty = db.Temat.Find(id).Post.Where(x => x.status > 1);
            if (forum_posty == null)
            {
                return HttpNotFound();
            }
            if (Session["lastPage"] == null || Session["lastPage"].ToString() != HttpContext.Request.Url.AbsolutePath)
                foreach (var post in forum_posty)
                {
                    if (post.idT == id)
                    {
                        post.Temat.odslony++;
                        db.SaveChanges();
                        break;
                    }
                }
            decimal maxPage = Math.Ceiling((decimal)forum_posty.Count() / (decimal)paginationLimit);

            ViewBag.Temat = db.Temat.Find(id).nazwa;

            if (page > maxPage) page = (int)maxPage;
            if (page <= 0) page = 1;

            ViewBag.paginationPost = paginationLimit;
            return View(forum_posty.Skip(paginationLimit * (page - 1)).Take(paginationLimit));
        }
        // GET: forum_posty/Create
        public ActionResult UtworzPost(int id)
        {
            if (!czyZalogowany()) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!czyIstniejeTemat(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ViewBag.tid = new SelectList(db.Temat, "id", "temat");
            ViewBag.uid = new SelectList(db.User, "id", "imie");
            ViewBag.Temat = db.Temat.Find(id).nazwa;
            ViewBag.tid2 = id;
            return View(db.Temat.Find(id).Post.First());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UtworzPost([Bind(Include = "id,tresc,uid,czas,status,tid")] Post forum_posty, int id)
        {
            if (!czyZalogowany()) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!czyIstniejeTemat(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            forum_posty.czas = DateTime.Now;
            forum_posty.idAutora = (int)Session["User"];
            forum_posty.idT = id;
            forum_posty.status = 2;
            if (ModelState.IsValid)
            {
                db.Post.Add(forum_posty);
                db.SaveChanges();

                return RedirectToAction("TematSzczegoly", new { id = forum_posty.idT });

            }
            ViewBag.Temat = forum_posty.Temat.nazwa;
            return RedirectToAction("TematSzczegoly", new { id = forum_posty.Temat.id });
        }

        public JsonResult getCreatePage(int id)
        {
            if (!czyZalogowany()) return Json(new { success = false });
            if (!czyIstniejeTemat(id)) return Json(new { success = false });
            ViewBag.tid = new SelectList(db.Temat, "id", "temat");
            ViewBag.uid = new SelectList(db.User, "id", "login");
            ViewBag.Temat = db.Temat.Find(id).nazwa;
            ViewBag.tid2 = id;
            return Json(new { success = true, html = RenderRazorViewToString("UtworzPost", db.Temat.Find(id).Post.First()) });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        private string RenderRazorViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var SW = new StringWriter())
            {
                var view = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, view.View, ViewData, TempData, SW);
                view.View.Render(viewContext, SW);
                view.ViewEngine.ReleaseView(ControllerContext, view.View);
                return SW.GetStringBuilder().ToString();
            }
        }
        public ActionResult AdminUtworzKategorie()
        {
            if (Session["Admin"] == null) return RedirectToAction("Index");
            return View();
        }

        // POST: Forum/Create
        // Aby zapewnić ochronę przed atakami polegającymi na przesyłaniu dodatkowych danych, włącz określone właściwości, z którymi chcesz utworzyć powiązania.
        // Aby uzyskać więcej szczegółów, zobacz https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminUtworzKategorie([Bind(Include = "id,nazwa,opis,kolejnosc,tylko_dla_zalogowanych,aktywna")] Kategoria forum_kategorie)
        {
            if (Session["Admin"] == null) return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                db.Kategoria.Add(forum_kategorie);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(forum_kategorie);
        }

        // GET: Forum/Edit/5
        public ActionResult AdminEdytujKategorie(int? id)
        {
            if (Session["Admin"] == null) return RedirectToAction("Index");
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Kategoria forum_kategorie = db.Kategoria.Find(id);
            if (forum_kategorie == null)
            {
                return HttpNotFound();
            }
            return View(forum_kategorie);
        }

        // POST: Forum/Edit/5
        // Aby zapewnić ochronę przed atakami polegającymi na przesyłaniu dodatkowych danych, włącz określone właściwości, z którymi chcesz utworzyć powiązania.
        // Aby uzyskać więcej szczegółów, zobacz https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminEdytujKategorie([Bind(Include = "id,nazwa,opis,kolejnosc,tylko_dla_zalogowanych,aktywna")] Kategoria forum_kategorie)
        {
            if (Session["Admin"] == null) return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                db.Entry(forum_kategorie).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(forum_kategorie);
        }
        public ActionResult AdminEdytujTemat(int? id)
        {
            if (Session["Admin"] == null) return RedirectToAction("Index");
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Temat forum_tematy = db.Temat.Find(id);
            if (forum_tematy == null)
            {
                return HttpNotFound();
            }
            ViewBag.idK = new SelectList(db.Kategoria, "id", "nazwa", forum_tematy.idK);
            ViewBag.uid = new SelectList(db.User, "id", "login", forum_tematy.idAutora);
            ViewBag.status = new SelectList(db.Status, "id", "nazwa", forum_tematy.status);
            ViewBag.Kategoria = forum_tematy.Kategoria.nazwa;
            return View(forum_tematy);
        }

        // POST: forum_tematy1/Edit/5
        // Aby zapewnić ochronę przed atakami polegającymi na przesyłaniu dodatkowych danych, włącz określone właściwości, z którymi chcesz utworzyć powiązania.
        // Aby uzyskać więcej szczegółów, zobacz https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminEdytujTemat([Bind(Include = "id,nazwa,czas,idAutora,status,odslony,idK")] Temat forum_tematy)
        {
            if (Session["Admin"] == null) return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                db.Entry(forum_tematy).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Tematy", new { id = forum_tematy.idK });
            }
            ViewBag.katid = new SelectList(db.Kategoria, "id", "nazwa", forum_tematy.idK);
            ViewBag.uid = new SelectList(db.User, "id", "imie", forum_tematy.idAutora);
            ViewBag.status = new SelectList(db.Status, "id", "nazwa", forum_tematy.status);
            ViewBag.Kategoria = forum_tematy.Kategoria.nazwa;
            return View(forum_tematy);
        }

        // GET: forum_posty/Edit/5
        public ActionResult EditPost(int? id)
        {
            if (!czyZalogowany()) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post forum_posty = db.Post.Find(id);
            if (forum_posty == null)
            {
                return HttpNotFound();
            }
            if ((int)Session["User"] != forum_posty.idAutora) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ViewBag.tid = new SelectList(db.Temat, "id", "temat", forum_posty.idT);
            ViewBag.uid = new SelectList(db.User, "id", "login", forum_posty.idAutora);
            ViewBag.Temat = forum_posty.Temat.nazwa;
            return View(forum_posty);
        }

        // POST: forum_posty/Edit/5
        // Aby zapewnić ochronę przed atakami polegającymi na przesyłaniu dodatkowych danych, włącz określone właściwości, z którymi chcesz utworzyć powiązania.
        // Aby uzyskać więcej szczegółów, zobacz https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost([Bind(Include = "id,tresc,idAutora,czas,status,idT")] Post forum_posty)
        {
            if (!czyZalogowany()) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            forum_posty.id = Int32.Parse(Session["changingPostId"].ToString());
            forum_posty.idT = Int32.Parse(Session["changingPostTid"].ToString());
            forum_posty.czas = DateTime.Parse(Session["changingPostCzas"].ToString());
            forum_posty.status = short.Parse(Session["changingPostStatus"].ToString());
            forum_posty.idAutora = Int32.Parse(Session["changingPostUid"].ToString());

            forum_posty.czas_edycji = DateTime.Now;
            if (ModelState.IsValid)
            {
                db.Entry(forum_posty).State = EntityState.Modified;
                db.SaveChanges();
                //czyszczenie wartosci sesji:
                Session["changingPostId"] = null;
                Session["changingPostTid"] = null;
                Session["changingPostCzas"] = null;
                Session["changingPostStatus"] = null;
                Session["changingPostUid"] = null;
                return RedirectToAction("TematSzczegoly", new { id = forum_posty.idT });
            }


            //czyszczenie wartosci sesji:
            Session["changingPostId"] = null;
            Session["changingPostTid"] = null;
            Session["changingPostCzas"] = null;
            Session["changingPostStatus"] = null;
            Session["changingPostUid"] = null;

            ViewBag.Temat = forum_posty.Temat.nazwa;
            ViewBag.tid = new SelectList(db.Temat, "id", "temat", forum_posty.idT);
            ViewBag.uid = new SelectList(db.User, "id", "imie", forum_posty.idAutora);
            return RedirectToAction("TematSzczegoly", new { id = forum_posty.Temat.id });
        }

        public ActionResult AdminEditPost(int? id)
        {
            if (Session["Admin"] == null) return RedirectToAction("Index");
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post forum_posty = db.Post.Find(id);
            if (forum_posty == null)
            {
                return HttpNotFound();
            }
            ViewBag.tid = new SelectList(db.Temat, "id", "temat", forum_posty.idT);
            ViewBag.uid = new SelectList(db.User, "id", "imie", forum_posty.idAutora);
            ViewBag.status = new SelectList(db.Status, "id", "nazwa", forum_posty.status);
            ViewBag.Temat = forum_posty.Temat.nazwa;
            return View(forum_posty);
        }

        // POST: forum_posty1/Edit/5
        // Aby zapewnić ochronę przed atakami polegającymi na przesyłaniu dodatkowych danych, włącz określone właściwości, z którymi chcesz utworzyć powiązania.
        // Aby uzyskać więcej szczegółów, zobacz https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminEditPost([Bind(Include = "id,tresc,idAutora,czas,status,idT,czas_edycji")] Post forum_posty)
        {
            if (Session["Admin"] == null) return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                db.Entry(forum_posty).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("TematSzczegoly", new { id = forum_posty.idT });
            }
            ViewBag.tid = new SelectList(db.Temat, "id", "temat", forum_posty.idT);
            ViewBag.uid = new SelectList(db.User, "id", "imie", forum_posty.idAutora);
            ViewBag.status = new SelectList(db.Status, "id", "nazwa", forum_posty.status);
            ViewBag.Temat = forum_posty.Temat.nazwa;
            return RedirectToAction("TematSzczegoly", new { id = forum_posty.Temat.id });
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
                return View("Logowanie");
            }
            var finded = db.User.Find(user.id);
            if (finded == null)
            {
                ViewBag.error = "Błędny login lub hasło";
                return View("Logowanie");
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
            if (uu.admin) Session["Admin"] = "true";
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

        public ActionResult Profil()
        {
            var user = db.User.Find(Session["User"]);
            if (user == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            return View(user);
        }
        [HttpPost]
        public ActionResult ChangePhoto(HttpPostedFileBase file)
        {
            var user = db.User.Find(Session["User"]);
            if (user == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (file != null && file.ContentLength > 0)
                try
                {
                    string toJpg = $"{user.id}.jpg";
                    string path = Path.Combine(Server.MapPath("~/media"),
                                               Path.GetFileName(toJpg));
                    file.SaveAs(path);
                    ViewBag.Message = "File uploaded successfully";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "ERROR:" + ex.Message.ToString();
                }
            else
            {
                ViewBag.Message = "You have not specified a file.";
            }
            return RedirectToAction("Profil");
        }
    }
}