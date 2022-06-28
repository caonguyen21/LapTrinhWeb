using PagedList;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanGiayDep.Models;

namespace WebBanGiayDep.Controllers
{

    public class AdminController : Controller
    {
        dbShopGiayDataContext data = new dbShopGiayDataContext();
        // GET: Admin
        public ActionResult Index()
        {
            if (Session["Username_Admin"] == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["Username_Admin"] != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        public ActionResult Login(FormCollection collection)
        {
            try
            {
                string Username = collection["txt_Username"];
                string Password = collection["txt_Password"];
                var AdminLogin = data.QUANLies.SingleOrDefault(a => a.TaiKhoanQL == Username && a.MatKhau == Password);
                if (ModelState.IsValid && AdminLogin != null)
                {
                    if (AdminLogin.TrangThai == true)// tài khoản không bị ban
                    {
                        //Lưu các thông tin vào Session
                        Session.Add("MaAdmin", AdminLogin.MaQL);
                        Session.Add("Username_Admin", Username);
                        Session.Add("HoTen_Admin", AdminLogin.HoTen);
                        Session.Add("Avatar_Admin", AdminLogin.Avatar);
                        //Lấy ra thông tin phân quyền của tài khoản vừa Login và vào Session
                        var PhanQuyen = data.PHANQUYENs.SingleOrDefault(p => p.MaQL == int.Parse(Session["MaAdmin"].ToString()));
                        Session.Add("PQ_QuanTriAdmin", PhanQuyen.QL_Admin);
                        Session.Add("PQ_KhachHang", PhanQuyen.QL_KhachHang);
                        Session.Add("PQ_YKienKhachHang", PhanQuyen.QL_YKienKhachHang);
                        Session.Add("PQ_DonHang", PhanQuyen.QL_DonHang);
                        Session.Add("PQ_ThuongHieu", PhanQuyen.QL_ThuongHieu);
                        Session.Add("PQ_NhaCungCap", PhanQuyen.QL_NhaCungCap);
                        Session.Add("PQ_LoaiGiay", PhanQuyen.QL_LoaiGiay);
                        Session.Add("PQ_SanPham", PhanQuyen.QL_SanPham);

                        return RedirectToAction("Index", "Admin");
                    }
                    else { return Content("<script>alert('Tài khoản quản trị của bạn đã bị khóa!');window.location='/Admin/Login';</script>"); }
                }
                else { return Content("<script>alert('Tên đăng nhập hoặc mật khẩu không đúng!');window.location='/Admin/Login';</script>"); }
            }
            catch
            {
                return Content("<script>alert('Đăng nhập thất bại!');window.location='/Admin/Login';</script>");
            }
        }
        public ActionResult Logout()
        {
            Session.RemoveAll();
            Session.Abandon();
            return RedirectToAction("Login");
        }
        public ActionResult Account()
        {
            //Chưa đăng nhập => Login
            if (Session["Username_Admin"] == null)
            {
                return RedirectToAction("Login");
            }
            int MaAdmin = int.Parse(Session["MaAdmin"].ToString());
            var ttad = data.QUANLies.SingleOrDefault(a => a.MaQL == MaAdmin);
            return View(ttad);
        }
        [HttpPost]
        public ActionResult Account(FormCollection collection)
        {
            try
            {
                string Email = collection["txt_Email"];
                string HoTen = collection["txt_HoTen"];
                string DienThoai = collection["txt_DienThoai"];
                string TaiKhoan = collection["Username_Admin"];

                int MaAdmin = int.Parse(Session["MaAdmin"].ToString());
                var ttad = data.QUANLies.SingleOrDefault(a => a.MaQL == MaAdmin);
                //Gán giá trị để chỉnh sửa
                ttad.EmailQL = Email;
                ttad.HoTen = HoTen;
                ttad.DienThoaiQL = DienThoai;
                HttpPostedFileBase FileUpload = Request.Files["FileUpload"];
                if (FileUpload != null && FileUpload.ContentLength > 0)//Kiểm tra đã chọn 1 file Upload để thực hiện tiếp
                {
                    string FileName = Path.GetFileName(FileUpload.FileName);
                    string Link = Path.Combine(Server.MapPath("/images/Upload/"), FileName);
                    if (FileUpload.ContentLength > 1 * 1024 * 1024)
                    {
                        return Content("<script>alert('Kích thước của tập tin không được vượt quá 1 MB!');window.location='/Admin/Account';</script>");
                    }
                    var DuoiFile = new[] { "jpg", "jpeg", "png", "gif" };
                    var FileExt = Path.GetExtension(FileUpload.FileName).Substring(1);
                    if (!DuoiFile.Contains(FileExt))
                    {
                        return Content("<script>alert('Chỉ được tải tập tin hình ảnh dạng (.jpg, .jpeg, .png, .gif)!');window.location='/Admin/Account';</script>");
                    }
                    FileUpload.SaveAs(Link);
                    ttad.Avatar = "/images/Upload/" + FileName;
                }
                //Thực hiện chỉnh sửa
                UpdateModel(ttad);
                data.SubmitChanges();
                return Content("<script>alert('Cập nhật thông tin cá nhân thành công!');window.location='/Admin/Account';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống.Vui lòng thử lại!');window.location='/Admin/Account';</script>");
            }
        }
        public ActionResult ListAdmin(int? page)
        {
            if (Session["Username_Admin"] == null)
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_QuanTriAdmin"].ToString()) == false)//Không đủ quyền hạn
            {
                return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Administrator !');window.location='/Admin/';</script>");
            }

            int PageSize = 3;//Chỉ lấy ra 3 dòng (3 Admin)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách Admin
            var PQ = (from pq in data.PHANQUYENs
                      orderby pq.MaQL descending
                      select pq).ToPagedList(PageNum, PageSize);
            return View(PQ);
        }
        [HttpPost]
        public ActionResult CreateAdmin()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_QuanTriAdmin"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Administrator !');window.location='/Admin/';</script>");

            return View();
        }
        public ActionResult SanPham(int? page)
        {
            int pageNumber = (page ?? 1);
            int pageSize = 7;
            return View(data.SANPHAMs.ToList().OrderBy(n => n.MaGiay).ToPagedList(pageNumber, pageSize));
        }
        [HttpGet]
        public ActionResult ThemMoiSanPham()
        {
            //lay ds tu table THUONGHIEU, sap xep theo Ten thuong hieu, chon lay gia tri MaThuongHieu, hien thi ten thuong hieu
            ViewBag.MaThuongHieu = new SelectList(data.THUONGHIEUs.ToList().OrderBy(n => n.TenThuongHieu), "MaThuongHieu", "TenThuongHieu");

            //lay ds tu table LoaiGiay, sap xep theo TenLoaiGiay, chon lay gia tri MaLoai, hien thi TenLoai
            ViewBag.MaLoai = new SelectList(data.LOAIGIAYs.ToList().OrderBy(n => n.TenLoai), "MaLoai", "TenLoai");

            //lay ds tu table NHACUNGCAP, sap xep theo TenNCC, chon lay gia tri MaNCC, hien thi TenNCC
            ViewBag.MaNCC = new SelectList(data.NHACUNGCAPs.ToList().OrderBy(n => n.TenNCC), "MaNCC", "TenNCC");

            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ThemMoiSanPham(SANPHAM sanpham, HttpPostedFileBase fileUpload)
        {
            ViewBag.MaThuongHieu = new SelectList(data.THUONGHIEUs.ToList().OrderBy(n => n.TenThuongHieu), "MaThuongHieu", "TenThuongHieu");
            ViewBag.MaLoai = new SelectList(data.LOAIGIAYs.ToList().OrderBy(n => n.TenLoai), "MaLoai", "TenLoai");
            ViewBag.MaNCC = new SelectList(data.NHACUNGCAPs.ToList().OrderBy(n => n.TenNCC), "MaNCC", "TenNCC");

            if (fileUpload == null)
            {
                ViewBag.ThongBao = "Vui lòng chọn ảnh bìa";
                return View();
            }
            else
            {
                if (ModelState.IsValid)
                {
                    //Luu ten file
                    var fileName = Path.GetFileName(fileUpload.FileName);
                    //Luu duong dan cua file
                    var path = Path.Combine(Server.MapPath("~/images"), fileName);
                    //Kiem tra hinh anh co ton tai chua
                    if (System.IO.File.Exists(path))
                    {
                        ViewBag.ThongBao = "Hình ảnh đã tồn tại";
                    }
                    else
                    {
                        //Luu hinh anh vao duong dan
                        fileUpload.SaveAs(path);
                    }
                    sanpham.AnhBia = fileName;
                    //luu vao csdl
                    data.SANPHAMs.InsertOnSubmit(sanpham);
                    data.SubmitChanges();
                }
                return RedirectToAction("SanPham");
            }
        }
        //Hien Thi Chi Tiet San Pham
        public ActionResult ChiTietSanPham(int id)
        {
            // lay sp theo ma sp
            SANPHAM sanPham = data.SANPHAMs.SingleOrDefault(n => n.MaGiay == id);
            ViewBag.MaGiay = sanPham.MaGiay;
            if (sanPham == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            return View(sanPham);
        }
        [HttpGet]
        public ActionResult XoaSanPham(int id)
        {
            //lay san pham can xoa
            SANPHAM sanPham = data.SANPHAMs.SingleOrDefault(n => n.MaGiay == id);
            ViewBag.MaGiay = sanPham.MaGiay;
            if (sanPham == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            return View(sanPham);
        }
        [HttpPost, ActionName("XoaSanPham")]
        public ActionResult XacNhanXoa(int id)
        {
            SANPHAM sanPham = data.SANPHAMs.SingleOrDefault(n => n.MaGiay == id);
            ViewBag.MaGiay = sanPham.MaGiay;
            if (sanPham == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.SANPHAMs.DeleteOnSubmit(sanPham);
            data.SubmitChanges();
            return RedirectToAction("SanPham");
        }
        //Chinh sua San pham
        [HttpGet]
        public ActionResult SuaSanPham(int id)
        {
            SANPHAM sanPham = data.SANPHAMs.SingleOrDefault(n => n.MaGiay == id);
            if (sanPham == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            //dua du lieu vao drop downlist TenTHuongHieu, TenLoai, TenNCC
            ViewBag.MaThuongHieu = new SelectList(data.THUONGHIEUs.ToList().OrderBy(n => n.TenThuongHieu), "MaThuongHieu", "TenThuongHieu");
            ViewBag.MaLoai = new SelectList(data.LOAIGIAYs.ToList().OrderBy(n => n.TenLoai), "MaLoai", "TenLoai");
            ViewBag.MaNCC = new SelectList(data.NHACUNGCAPs.ToList().OrderBy(n => n.TenNCC), "MaNCC", "TenNCC");
            return View(sanPham);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SuaSanPham(int id, HttpPostedFileBase fileUpload)
        {
            SANPHAM sp = data.SANPHAMs.SingleOrDefault(n => n.MaGiay == id);

            ViewBag.MaThuongHieu = new SelectList(data.THUONGHIEUs.ToList().OrderBy(n => n.TenThuongHieu), "MaThuongHieu", "TenThuongHieu");
            ViewBag.MaLoai = new SelectList(data.LOAIGIAYs.ToList().OrderBy(n => n.TenLoai), "MaLoai", "TenLoai");
            ViewBag.MaNCC = new SelectList(data.NHACUNGCAPs.ToList().OrderBy(n => n.TenNCC), "MaNCC", "TenNCC");
            if (fileUpload == null)
            {
                ViewBag.ThongBao = "Vui lòng chọn ảnh bìa";
                return View();
            }
            else
            {
                if (ModelState.IsValid)
                {
                    //Luu ten file
                    var fileName = Path.GetFileName(fileUpload.FileName);
                    //Luu duong dan cua file
                    var path = Path.Combine(Server.MapPath("~/images"), fileName);
                    //Kiem tra hinh anh co ton tai chua
                    if (System.IO.File.Exists(path))
                    {
                        ViewBag.ThongBao = "Hình ảnh đã tồn tại";
                    }
                    else
                    {
                        //Luu hinh anh vao duong dan
                        fileUpload.SaveAs(path);
                    }
                    sp.AnhBia = fileName;
                    //luu vao csdl
                    UpdateModel(sp);
                    data.SubmitChanges();
                }
                return RedirectToAction("SanPham");
            }
        }
        [HttpGet]
        //y kien khach hang
        public ActionResult ykienkhachhang(int? page)
        {
            int pageNumber = (page ?? 1);
            int pageSize = 5;
            return View(data.YKIENKHACHHANGs.ToList().OrderBy(n => n.MAYKIEN).ToPagedList(pageNumber, pageSize));
        }
        //Hien thi y kien
        [HttpGet]
        public ActionResult Xoaykienkhachhang(int id)
        {
            YKIENKHACHHANG ykienkhachhang = data.YKIENKHACHHANGs.SingleOrDefault(n => n.MAYKIEN == id);
            ViewBag.MAYKIEN = ykienkhachhang.MAYKIEN;
            if (ykienkhachhang == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            return View(ykienkhachhang);
        }
        [HttpPost, ActionName("Xoaykienkhachhang")]
        public ActionResult Xacnhanxoa(int id)
        {
            //Lay ra y kien can xoa
            YKIENKHACHHANG ykienkhachhang = data.YKIENKHACHHANGs.SingleOrDefault(n => n.MAYKIEN == id);
            ViewBag.MAYKIEN = ykienkhachhang.MAYKIEN;
            if (ykienkhachhang == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.YKIENKHACHHANGs.DeleteOnSubmit(ykienkhachhang);
            data.SubmitChanges();
            return RedirectToAction("Ykienkhachhang");
        }
        //quan ly thuong hieu
        public ActionResult ThuongHieu(int? page)
        {
            int pageNumber = (page ?? 1);
            int pageSize = 9;
            return View(data.THUONGHIEUs.ToList().OrderBy(n => n.MaThuongHieu).ToPagedList(pageNumber, pageSize));
        }
        //THEM THUONG HIEU
        [HttpGet]
        public ActionResult ThemMoiThuongHieu()
        {
            //lay ds tu table THUONGHIEU, sap xep theo Ten thuong hieu, chon lay gia tri MaThuongHieu, hien thi ten thuong hieu
            ViewBag.MaThuongHieu = new SelectList(data.THUONGHIEUs.ToList().OrderBy(n => n.TenThuongHieu), "MaThuongHieu", "TenThuongHieu");
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ThemMoiThuongHieu(THUONGHIEU tHUONGHIEU)
        {
            //lay ds tu table THUONGHIEU, sap xep theo Ten thuong hieu, chon lay gia tri MaThuongHieu, hien thi ten thuong hieu
            ViewBag.MaThuongHieu = new SelectList(data.THUONGHIEUs.ToList().OrderBy(n => n.TenThuongHieu), "MaThuongHieu", "TenThuongHieu");
            data.THUONGHIEUs.InsertOnSubmit(tHUONGHIEU);
            //save vao csdl
            data.SubmitChanges();
            return RedirectToAction("ThuongHieu");
        }
        //chi tiet thuong hieu
        public ActionResult ChiTietThuongHieu(int id)
        {
            // lay thuong hieu theo ma th
            THUONGHIEU tHUONGHIEU = data.THUONGHIEUs.SingleOrDefault(n => n.MaThuongHieu == id);
            ViewBag.MaThuongHieu = tHUONGHIEU.MaThuongHieu;
            if (tHUONGHIEU == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            return View(tHUONGHIEU);
        }
        //xoa thuong hieu
        [HttpGet]
        public ActionResult XoaThuongHieu(int id)
        {
            //lay san pham can xoa
            THUONGHIEU tHUONGHIEU = data.THUONGHIEUs.SingleOrDefault(n => n.MaThuongHieu == id);
            ViewBag.MaThuongHieu = tHUONGHIEU.MaThuongHieu;
            if (tHUONGHIEU == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            return View(tHUONGHIEU);
        }
        [HttpPost, ActionName("XoaThuongHieu")]
        public ActionResult XacNhanXoaThuongHieu(int id)
        {
            THUONGHIEU tHUONGHIEU = data.THUONGHIEUs.SingleOrDefault(n => n.MaThuongHieu == id);
            ViewBag.MaThuongHieu = tHUONGHIEU.MaThuongHieu;
            if (tHUONGHIEU == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.THUONGHIEUs.DeleteOnSubmit(tHUONGHIEU);
            data.SubmitChanges();
            return RedirectToAction("ThuongHieu");
        }
        //sua thong tin thuong hieu
        [HttpGet]
        public ActionResult SuaThuonghieu(int id)
        {
            THUONGHIEU tHUONGHIEU = data.THUONGHIEUs.SingleOrDefault(n => n.MaThuongHieu == id);
            if (tHUONGHIEU == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            //dua du lieu vao drop downlist TenTHuongHieu, TenLoai, TenNCC
            ViewBag.MaThuongHieu = new SelectList(data.THUONGHIEUs.ToList().OrderBy(n => n.TenThuongHieu), "MaThuongHieu", "TenThuongHieu");
            return View(tHUONGHIEU);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SuaThuonghieuu(int id)
        {
            THUONGHIEU tHUONGHIEU = data.THUONGHIEUs.SingleOrDefault(n => n.MaThuongHieu == id);
            ViewBag.MaThuongHieu = new SelectList(data.THUONGHIEUs.ToList().OrderBy(n => n.TenThuongHieu), "MaThuongHieu", "TenThuongHieu");
            //luu vao csdl
            UpdateModel(tHUONGHIEU);
            data.SubmitChanges();
            return RedirectToAction("ThuongHieu");
        }
    }
}