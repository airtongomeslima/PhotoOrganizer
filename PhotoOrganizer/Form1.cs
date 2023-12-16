using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhotoOrganizer
{
    public partial class Form1 : Form
    {
        string sourceFolder = "H:\\OldOneDrive\\Pictures\\Câmera";
        string outputFolder = "G:\\OutputImageOrganizer";
        HashSet<string> hashSet = new HashSet<string>();
        int totalFiles = 0;
        int totalProcessed = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            //open folder dialog to select a folder and set to sourceFolder string
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                sourceFolder = folderBrowserDialog1.SelectedPath;
                textBox1.Text = sourceFolder;
            }
        }

        private void textBox2_MouseClick(object sender, MouseEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                outputFolder = folderBrowserDialog1.SelectedPath;
                textBox2.Text = outputFolder;
            }
        }

        private async void bt_execute_Click(object sender, EventArgs e)
        {

            await Task.Run(() =>
            {
                //load hasehsFiles.txt
                string hashesFiles = LoadFile(outputFolder + "\\hashesFiles.txt");
                string[] hashes = hashesFiles.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                //load copiedFiles.txt
                string copiedFiles = LoadFile(outputFolder + "\\copiedFiles.txt");
                string[] copied = copiedFiles.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                //load ignoredFiles.txt
                string ignoredFiles = LoadFile(outputFolder + "\\ignoredFiles.txt");
                string[] ignored = ignoredFiles.Split(new[] { Environment.NewLine }, StringSplitOptions.None);


                //check if sourceFolder and outputFolder are set
                if (sourceFolder == null || outputFolder == null)
                {
                    MessageBox.Show("Please select a source folder and an output folder.");
                }
                else
                {
                    //get all files in sourceFolder
                    string[] files = System.IO.Directory.GetFiles(sourceFolder, "*.*", System.IO.SearchOption.AllDirectories);

                        totalFiles = files.Length;
                        totalProcessed = 0;

                    this.Invoke(new Action(() =>
                    {
                        countlbl.Text = totalProcessed.ToString() + " / " + totalFiles.ToString();
                        progressBar1.Maximum = totalFiles;
                        progressBar1.Value = 0;
                    }));

                    //loop through all files
                    foreach (string file in files)
                    {
                        try
                        {
                            //check if file is in ignoredFiles
                            if (ignored.Contains(file))
                            {
                                continue;
                            }

                            //check if file is in copiedFiles
                            if (copied.Contains(file))
                            {
                                continue;
                            }

                            //get file extension
                            string extension = System.IO.Path.GetExtension(file);

                            //check if file is a jpg
                            //get file hash
                            string hash = GetFileHash(file);
                            if (hash != null)
                            {
                                //check if file is in hashesFiles
                                if (hashes.Contains(hash))
                                {
                                    continue;
                                }

                                //check if hash is already in hashSet
                                if (!hashSet.Contains(hash))
                                {
                                    //add hash to hashSet
                                    hashSet.Add(hash);
                                    WriteToLog(hash, "hashesFiles");

                                    //get date taken from file
                                    DateTime dateTaken = GetDateTakenFromImage(file);

                                    //create folder in outputFolder with year and month
                                    string year = dateTaken.Year.ToString();
                                    string month = dateTaken.Month.ToString();
                                    string folderName = year + "\\" + month;
                                    string folderPath = outputFolder + "\\" + folderName;
                                    System.IO.Directory.CreateDirectory(folderPath);

                                    //copy file to folder
                                    string fileName = System.IO.Path.GetFileName(file);
                                    string destFile = System.IO.Path.Combine(folderPath, fileName);
                                    System.IO.File.Copy(file, destFile, true);
                                    WriteToLog(folderPath + "\\" + fileName, "copiedFiles");
                                }
                                else
                                {
                                    WriteToLog($"DUPLICATE {file}", "duplicatedFiles");
                                }
                            }
                            else
                            {
                                WriteToLog($"HASHFAIL {file}", "failFiles");
                            }
                        }
                        catch (Exception)
                        {
                            WriteToLog($"FAIL {file}", "failFiles");
                        }

                        totalProcessed++;
                        this.Invoke(new Action(() => {
                            countlbl.Text = totalProcessed.ToString() + " / " + totalFiles.ToString();
                            progressBar1.Value = totalProcessed;
                        }));
                    }
                }
            });

            MessageBox.Show("Done!");
        }

        public string LoadFile(string fileName)
        {
            //load file
            if (System.IO.File.Exists(fileName))
            {
                string text = System.IO.File.ReadAllText(fileName);
                return text;
            }
            else
            {
                return "";
            }
        }

        public void WriteToLog(string text, string fileName)
        {
            //write to log
            string logFile = outputFolder + "\\" + fileName + ".txt";
            System.IO.File.AppendAllText(logFile, text + Environment.NewLine);
        }


        private string GetFileHash(string file)
        {
            //get file hash
            string hash = "";
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(file))
                {
                    hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
            return hash;
        }

        private DateTime GetDateTakenFromImage(string file)
        {
            try
            {
                //get date taken from image
                System.Drawing.Imaging.PropertyItem propItem = null;
                using (var fs = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    var myImage = System.Drawing.Image.FromStream(fs, false, false);
                    propItem = myImage.GetPropertyItem(36867);
                }
                string dateTaken = System.Text.Encoding.UTF8.GetString(propItem.Value).Trim();
                string year = dateTaken.Substring(0, 4);
                string month = dateTaken.Substring(5, 2);
                string day = dateTaken.Substring(8, 2);
                DateTime date = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), Convert.ToInt32(day));
                return date;
            }
            catch (Exception)
            {
                FileInfo fileInfo = new FileInfo(file);
                return fileInfo.CreationTime;
            }
        }
    }
}
