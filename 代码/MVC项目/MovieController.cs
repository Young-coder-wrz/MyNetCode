using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcProject.Model;
using Webdiyer.WebControls.Mvc;

namespace MvcProject.Controllers
{
    public class MovieController : Controller
    {
        // GET: Movie
        public MoviesEntities context = new MoviesEntities();//实例化EF上下文对象
        public ActionResult Index(string cid,string tid,int pageindex=1)
        {
            //EF两表联合的几种方法 1.linqtosql 2.lambda 3.sql 4.黑科技(视图)
            #region 单表查询
            //var list = (from model in context.movie
            //            select model).ToList();//linqtosql
            //var list1 = context.movie.Select(m => m).ToList();//lambda
            //var sql = "select m.*,mt.typeName from movie as m join movieType as mt on m.typeId=mt.typeId";//sql
            #endregion
            #region 多表查询
            //var list_linq = (from m in context.movie
            //                from mt in context.movieType
            //                where m.typeId == mt.typeId
            //                select new Moviedetail
            //                {
            //                    mid = m.mid,
            //                    mname = m.mname,
            //                    createtime = m.createtime,
            //                    typeId = m.typeId,
            //                    typeName = mt.typeName
            //                }).ToList();
            //var list_linq = (from m in context.movie
            //                 join mt in context.movieType
            //                 on m.typeId equals mt.typeId
            //                 select new Moviedetail
            //                 {
            //                     mid = m.mid,
            //                     mname = m.mname,
            //                     createtime = m.createtime,
            //                     typeId = m.typeId,
            //                     typeName = mt.typeName
            //                 }).ToList();
            #endregion
            //检索
            string txtname = Request["txtname"];
            string txttime = Request["txttime"];
            var list_linq = (from m in context.movie
                             join mt in context.movieType on m.typeId equals mt.typeId
                             join c in context.country on m.cid equals c.cid
                             select new Moviedetail
                             {
                                 mid = m.mid,
                                 mname = m.mname,
                                 createtime = m.createtime,
                                 typeId = m.typeId,
                                 cid=m.cid,
                                 typeName = mt.typeName,
                                 cname = c.cname
                             }).ToList();
            if(!string.IsNullOrEmpty(txtname))
            {
                list_linq = list_linq.Where(md => md.mname.Contains(txtname)).ToList();
            }
            if(!string.IsNullOrEmpty(txttime))
            {
                list_linq = list_linq.Where(md => md.createtime.Equals(Convert.ToDateTime(txttime))).ToList();
                //context.movie.Skip(2).Take(3).ToList(); 
            }
            ViewBag.num_cid = 0;
            ViewBag.num_tid = 0;
            if (!string.IsNullOrEmpty(cid))
            {
                int num_cid = Convert.ToInt32(cid);
                ViewBag.num_cid = num_cid;
                if (num_cid>0)
                {
                    list_linq = list_linq.Where(md =>md.cid== num_cid).ToList();
                }
            }
            if(!string.IsNullOrEmpty(tid))
            {
                int num_tid = Convert.ToInt32(tid);
                ViewBag.num_tid = num_tid;
                if (num_tid>0)
                {
                    list_linq = list_linq.Where(md => md.typeId == num_tid).ToList();
                }
            }
            ViewBag.country_list = context.country.ToList();
            ViewBag.type_list =context.movieType.ToList();
            return View(list_linq.OrderBy(mt=>mt.mid).ToPagedList(pageindex, 5));
        }
        public ActionResult Delete(string id)
        {
            //routeattribute里面的id值自动装配到参数中
            movie mov = context.movie.Find(Convert.ToInt32(id));//Find方法根据要查找的实体的主键值查对象
            context.movie.Remove(mov);//ef删除对象
            context.SaveChanges();//同步数据库
            return Content("<script>alert('删除成功!');window.location.href='/Movie/Index';</script>");
        }
        public ActionResult AddMovie()
        {
            return View();
        }
        [HttpPost]
        public ActionResult AddMovie(movie mov)
        {
            //强类型界面自动装配一个movie对象
            #region 方法一 Request接收
            //string mname = Request["mname"];
            //DateTime time =Convert.ToDateTime(Request["createtime"]);
            //movie m = new movie() { mname = mname, createtime = time };//实例化待插入对象
            //context.movie.Add(m);
            //context.SaveChanges();
            #endregion
            #region 方法二 自动装配
            context.movie.Add(mov);
            context.SaveChanges();
            #endregion
            return Content("<script>alert('添加成功!');window.location.href='/Movie/Index';</script>");
        }
        public ActionResult Edit(string id)
        {
            //根据路由id返回实体对象给view填充value值
            int mid = Convert.ToInt32(id);
            //movie mov = context.movie.FirstOrDefault(m => m.mid == mid );
            movie mov = context.movie.Find(mid);
            return View(mov);
        }
        [HttpPost]
        public ActionResult Edit(movie mov)
        {
            if(mov!=null)
            {
                //使用TryUpdateModel更新 注:只能更新ef查找出来的实体对象 不能更新view传回的实体对象
                movie m = context.movie.Find(mov.mid);
                m.mname = mov.mname;
                m.createtime = mov.createtime;
                TryUpdateModel(m);
                //context.Entry(mov).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();
                return Content("<script>alert('更新成功!');window.location.href='/Movie/Index';</script>");
            }
            return RedirectToAction("Index");
        }
        public ActionResult SelectByName(string searchname)
        {
            if (!string.IsNullOrEmpty(searchname))
            {
                var list = (from m in context.movie
                            join mt in context.movieType
                            on m.typeId equals mt.typeId
                            where m.mname.Contains(searchname)
                            select new Moviedetail
                            {
                                mid = m.mid,
                                mname = m.mname,
                                createtime = m.createtime,
                                typeId=m.typeId,
                                typeName = mt.typeName
                            }).ToList();
                    return Json(list, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View();
            }
        }
        public ActionResult SelectByType(string typeId)
        {
            int tid = Convert.ToInt32(typeId);
            var list = context.movie.Where(m => m.typeId == tid).Join(context.movieType, m => m.typeId, mt => mt.typeId, (m, mt) => new Moviedetail
            {
                mid = m.mid,
                mname=m.mname,
                typeId=m.typeId,
                createtime=m.createtime,
                typeName=mt.typeName
            }).ToList();
            return Json(list,JsonRequestBehavior.AllowGet);
        }
    }
}