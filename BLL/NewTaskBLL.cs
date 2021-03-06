﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using System.IO;
using OfficeHelper;
using Model.Enum;
using DAL;
using log4net;
using System.Reflection;

namespace BLL
{
    /// <summary>
    /// 
    /// </summary>
    public class NewTaskBLL
    {
        //这些标签都移动到配置文件中???
        private readonly static string OFFICE_TAG_DATE = "Date";
        private readonly static string OFFICE_TAG_NAME = "Name";
        private readonly static string OFFICE_TAG_TASKNO = "TaskNo";
        private readonly static string OFFICE_TAG_AUTHOR = "Author";

        private Task task;
        private string path;
        private string author;

        private ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="path"></param>
        public NewTaskBLL(Task task, string workspace, string author)
        {
            this.task = task;
            this.path = workspace;
            this.author = author;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Create()
        {
            log.Info("开始创建任务");
            DirectoryInfo taskSpace = Directory.CreateDirectory(path);
            if (!taskSpace.Exists)
            {
                taskSpace.Create();
                log.Info("创建工作区目录");
            }
            //创建任务目录
            DirectoryInfo taskDir = taskSpace.CreateSubdirectory(this.task.Description);
            log.Info("创建任务目录");
            if (taskDir.Exists)
            {
                //创建任务目录
                //taskDir.Create();
                log.Info("创建任务文件");
                //创建任务文件
                CreateTemplate(taskDir);

                
                //写入数据文件
                TaskDAL taskDal = new TaskDAL();
                int  ire = taskDal.Insert(this.task);
                if (ire > 0)
                {
                    log.Info("写入数据文件成功");
                }
                else
                {
                    log.Info("写入数据文件失败");
                }

                return true;
            }
            return false;
        }

        private void CreateTaskSpace()
        {
        }

        /// <summary>
        /// 创建模板文件
        /// 
        /// </summary>
        private void CreateTemplate(DirectoryInfo taskDir)
        {
            //TODO：需要重构
            string templatePath = AppDomain.CurrentDomain.BaseDirectory + SysData.FileName.TEMPLATE_PATH + @"\";
            string destDirName = taskDir.FullName + @"\";
            string sourceFile = "";
            string destFile = "";
            try
            {
                foreach (TaskFile file in this.task.Files)
                {
                    //设计文档
                    if (file.Type == TaskFileEnum.Design)
                        CreateDesignFile(templatePath, destDirName);

                    //自测文档
                    if (file.Type == TaskFileEnum.Test)
                        CreateTestFile(templatePath, destDirName);

                    //修改列表
                    if (file.Type == TaskFileEnum.Xls)
                        CreateModifyFile(templatePath, destDirName);

                    //DEV-SQL
                    if (file.Type == TaskFileEnum.DevSql)
                        CreateTextFile(templatePath, destDirName, "DEV-SQL.sql");
                    if (file.Type == TaskFileEnum.DML)
                    {
                        string fileName = this.task.Sys.ToString() + "-DML-" + 
                            this.task.Description + "-" + this.author + "-" +
                            DateTime.Today.ToString("yyyyMMdd") + ".sql";
                        CreateTextFile(templatePath, destDirName, fileName);
                    }
                    if (file.Type == TaskFileEnum.README)
                    {
                        string content = task.Seq + " " + task.Name + " " + this.author;
                        CreateTextFile(templatePath, destDirName, "readme.txt", content);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Info("创建失败");
            }
            finally
            {
                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templatePath"></param>
        /// <param name="destDirName"></param>
        private void CreateModifyFile(string templatePath, string destDirName)
        {

            string sourceFile = "";
            string destFile = "";
            sourceFile = templatePath + SysData.FileName.XLS;
            destFile = destDirName + this.task.Description + @"-修改列表.xls";
            File.Copy(sourceFile, destFile);
            log.Info("修改列表创建成功");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templatePath"></param>
        /// <param name="destDirName"></param>
        private void CreateTestFile(string templatePath, string destDirName)
        {
            string sourceFile = "";
            string destFile = "";
            sourceFile = templatePath + SysData.FileName.TEST;
            destFile = destDirName + this.task.Description + DateTime.Now.ToString(@"-自测报告yyyyMMdd") + ".doc";
            File.Copy(sourceFile, destFile);
            log.Info("自测文档创建成功");

            WordHelper wHelper = new WordHelper();
            Microsoft.Office.Interop.Word._Document oDoc = wHelper.Load(destFile);

            //获取模板中所有的书签 
            Microsoft.Office.Interop.Word.Bookmarks bookMarks = oDoc.Bookmarks;
            foreach (Microsoft.Office.Interop.Word.Bookmark bm in bookMarks)
            {
                bm.Select();
                bm.Range.Text = GetContentFromTag(bm.Name);
            }

            object filename = destFile;
            try
            {
                wHelper.Save(oDoc);
                wHelper.Close(oDoc);
            }
            catch (Exception ex)
            {
                wHelper.Close(oDoc);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templatePath"></param>
        /// <param name="destDirName"></param>
        private void CreateDesignFile(string templatePath, string destDirName)
        {
            string sourceFile = "";
            string destFile = "";
            sourceFile = templatePath + SysData.FileName.DESIGN;
            destFile = destDirName + this.task.Description + DateTime.Now.ToString(@"-设计说明书yyyyMMdd") + ".doc";
            File.Copy(sourceFile, destFile);
            log.Info("设计文档创建成功");

            WordHelper wHelper = new WordHelper();
            Microsoft.Office.Interop.Word._Document oDoc = wHelper.Load(destFile);

            //获取模板中所有的书签 
            Microsoft.Office.Interop.Word.Bookmarks bookMarks = oDoc.Bookmarks;
            foreach (Microsoft.Office.Interop.Word.Bookmark bm in bookMarks)
            {
                bm.Select();
                bm.Range.Text = GetContentFromTag(bm.Name);
            }

            object filename = destFile;
            try
            {
                wHelper.Save(oDoc);
                wHelper.Close(oDoc);
            }
            catch (Exception ex)
            {
                wHelper.Close(oDoc);
            }
        }

        /// <summary>
        /// 根据office标签得到需要设置的内容
        /// </summary>
        /// <param name="officeBookMark">标签名称</param>
        /// <returns>表单代表的内容</returns>
        private string GetContentFromTag(string officeBookMark)
        {
            string content = "";
            if (officeBookMark.Contains(OFFICE_TAG_DATE))
            {
                content = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else if (officeBookMark.Contains(OFFICE_TAG_NAME))
            {
                content = this.task.Name;
            }
            else if (officeBookMark.Contains(OFFICE_TAG_TASKNO))
            {
                content = this.task.Seq;
            }
            else if (officeBookMark.Contains(OFFICE_TAG_AUTHOR))
            {
                content = this.author;
            }
            return content;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templatePath"></param>
        /// <param name="destDirName"></param>
        /// <param name="fileName"></param>
        private void CreateTextFile(string templatePath, string destDirName,string fileName)
        {
            CreateTextFile(templatePath, destDirName, fileName, null);
        }

        private void CreateTextFile(string templatePath, string destDirName, string fileName, string content)
        {
            string sourceFile = templatePath + SysData.FileName.DEV;
            string destFile = destDirName + fileName;
            File.Copy(sourceFile, destFile);
            //有需要写入文件的内容
            if (!string.IsNullOrEmpty(content))
            {
                using (FileStream fs = new FileStream(destFile, FileMode.Append))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    sw.Write(content + Environment.NewLine);
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }

            }
            log.Info(fileName + "创建成功");
        }

    }
}
