using FrontToBack.DataAccessLayer;
using FrontToBack.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FrontToBack.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    public class ExpertController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;
        private readonly string _defaultImg;   

        public ExpertController(AppDbContext dbContext, IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _environment = environment;
            _defaultImg = "defaultImagePerson.png";
        }

        public async Task<IActionResult> Index(int page=1)
        {
            if (page <1)
                return NotFound();
            if (((page - 1) * 10) >= await _dbContext.Experts.CountAsync())
                page--;
            var totalPageCount = Math.Ceiling((decimal)await _dbContext.Experts.CountAsync() / 10);
            if (page > totalPageCount)
                return NotFound();

            ViewBag.totalPageCount = totalPageCount;
            ViewBag.currentPage = page;
            var experts = await _dbContext.Experts.Skip((page - 1) * 10).Take(10).ToListAsync();

            return View(experts);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return BadRequest();
            var expert = await _dbContext.Experts.Include(x=>x.Position).FirstOrDefaultAsync(x => x.Id == id);
            if (expert == null)
                return NotFound();

            return View(expert);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Positions =await _dbContext.Positions.ToListAsync();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expert expert)
        {
            ViewBag.Positions = await _dbContext.Positions.ToListAsync();
            if (!ModelState.IsValid)
                return View();
            if (expert.Photo != null)
            {
                if (!expert.Photo.ContentType.Contains("image"))
                {
                    ModelState.AddModelError("Photo", "File must to be a Photo!");
                    return View();
                }

                if (expert.Photo.Length > 1024 * 1000)
                {
                    ModelState.AddModelError("Photo", "The size of the photo cannot be more than one MegaByte!");
                    return View();
                }
                var webRootPath = _environment.WebRootPath;
                var fileName = $"{Guid.NewGuid()}-{expert.Photo.FileName}";
                var path = Path.Combine(webRootPath, "img", fileName);
                using (var fileStream = new FileStream(path, FileMode.CreateNew))
                {
                    await expert.Photo.CopyToAsync(fileStream);
                }
                expert.Image = fileName;
            }
            else
            {
                expert.Image = _defaultImg;
            }
           
            await _dbContext.Experts.AddAsync(expert);
            await _dbContext.SaveChangesAsync();
           
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id == null)
                return NotFound();

            var expert = await _dbContext.Experts.Include(x => x.Position).FirstOrDefaultAsync(x=>x.Id==id);
            if (expert == null)
                return NotFound();
            ViewBag.Positions = await _dbContext.Positions.ToListAsync();
            ViewBag.DefaultImg = _defaultImg;
            return View(expert);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id,Expert expert)
        {
            ViewBag.Positions = await _dbContext.Positions.ToListAsync();
            if (id == null)
                return NotFound();

            if (id != expert.Id)
                return BadRequest();
            var existExpert = await _dbContext.Experts.FindAsync(id);
            if (existExpert == null)
                return NotFound();
            if (expert.Photo != null)
            {
                if (!expert.Photo.ContentType.Contains("image"))
                {
                    ModelState.AddModelError("Photo", "File must to be a Photo!");
                    return View(existExpert);
                }
                if (expert.Photo.Length > 1024 * 1000)
                {
                    ModelState.AddModelError("Photo", "The size of the photo cannot be more than one MegaByte!");
                    return View(existExpert);
                }

                var webRootPath = _environment.WebRootPath;
                var existPath = Path.Combine(webRootPath, "img", existExpert.Image);
                if (System.IO.File.Exists(existPath) && existExpert.Image != _defaultImg)
                {
                    System.IO.File.Delete(existPath);
                }
                var fileName = $"{Guid.NewGuid()}-{expert.Photo.FileName}";
                var path = Path.Combine(webRootPath, "img", fileName);
                using (var fileStream = new FileStream(path, FileMode.CreateNew))
                {
                    await expert.Photo.CopyToAsync(fileStream);
                }
                expert.Image = fileName;
                existExpert.Image = expert.Image;
            }
            existExpert.Name = expert.Name;
            existExpert.Surname = existExpert.Surname;
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {           
            if (id == null)
                return BadRequest();
            var expert = await _dbContext.Experts.FindAsync(id);
            if (expert == null)
                return Json(new { status = 404 });
            var webRootPath = _environment.WebRootPath;
            var path = Path.Combine(webRootPath, "img",expert.Image);
            if (System.IO.File.Exists(path) && expert.Image!=_defaultImg)
            {
                System.IO.File.Delete(path);
            }
            _dbContext.Remove(expert);
            await _dbContext.SaveChangesAsync();
            return Json(new { status = 200 });
        }
        public async Task<IActionResult> DeletePhoto(int? id)
        {
            if (id == null)
                return BadRequest();
            var expert = await _dbContext.Experts.FindAsync(id);
            if (expert == null)
                return Json(new { status = 404 });
            var webRootPath = _environment.WebRootPath;
            var path = Path.Combine(webRootPath, "img", expert.Image);
            if (System.IO.File.Exists(path) && expert.Image!=_defaultImg)
            {
                System.IO.File.Delete(path);
            }
            expert.Image = _defaultImg;
            await _dbContext.SaveChangesAsync();
            return Json(new { status = 200 });
        }
    }
}
