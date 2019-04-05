using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using System.Collections.Generic;

namespace Nastenko05
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       
            // Process
            private string selectedProcessId;
            private ListSortDirection processSortDirection;
            private int processSortByColumnIndex;
            private bool isProcessSortRequired;

            // Module
            private string selectedModuleId;

            private bool IsProcessSelected
            {
                get { return (!string.IsNullOrEmpty(selectedProcessId)); }
            }
            private bool IsModuleSelected
            {
                get { return (!string.IsNullOrEmpty(selectedModuleId)); }
            }

            public MainWindow()
            {
                InitializeComponent();
                GetProcessList("MainWindow");

            }
            private class MyData
            {
                public string ProcessName { set; get; }
                public string Id { set; get; }
                public string IsResponding { set; get; }
                public string CpuUsage { set; get; }
                public double DoubleCpuUsage { set; get; }
                public string Memory { set; get; }
                public long NumericMemory { set; get; }
                public string StartDate { set; get; }
                public string StartTime { set; get; }
                public string ModuleName { set; get; }
                public string MainModuleFileName { set; get; }
                public string UserName { set; get; }
                public string State { set; get; }
            }
            private class CpuThread
            {
                public int Id { set; get; }
                public float CpuUsage { set; get; }
            }
            List<CpuThread> CpuThreadList;
        
            private void btnAcceptButton_Click(object sender, RoutedEventArgs e)
            {
                this.Close();
            }
            private void btnKill_Click(object sender, RoutedEventArgs e) // зупиняємо процес
            {
                
                Process p = GetSelectedProcess();
                if (p != null)
                {
                    try
                    {
                        p.Kill();                      
                        GetProcessList("Kill");
                        ProcessClearSelectedRow();
                    }
                    catch { }
                }
            }
            private void btnOpenFolder_Click(object sender, RoutedEventArgs e) // для відкриття папки процесу
            {
                // відкрити папку, з якої процес був запущений 
                Process p = GetSelectedProcess();
                if (p != null)
                {
                    string fileName = System.IO.Path.GetDirectoryName(p.MainModule.FileName);
                    Process.Start("explorer.exe", fileName);                    
                    ProcessSetSelectedRow();
                }
            }
            private void btnUpdateProcess_Click(object sender, RoutedEventArgs e)
            {
                GetProcessList("Перечитати");
                if (isProcessSortRequired)
                {
                    // Сортування :

                    var column = dgProcesses.Columns[processSortByColumnIndex];                    
                    dgProcesses.Items.SortDescriptions.Clear();
                    // Додаємо вірні значення, розраховані в dgProcesses_Sorting
                    // після чергового сортування :
                    dgProcesses.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, processSortDirection));                    
                    foreach (var col in dgProcesses.Columns)
                    {
                        col.SortDirection = null;
                    }
                    column.SortDirection = processSortDirection;

                    // Оновлення
                    dgProcesses.Items.Refresh();
                }
                
                ProcessSetSelectedRow();
            }
            private void SetVisibility(Visibility visibility)
            {
                
                txtModuleCount.Visibility = visibility;
                txtThreadCount.Visibility = visibility;
               
                btnKill.IsEnabled = IsProcessSelected;
                btnOpenFolder.IsEnabled = IsProcessSelected;
            }

            #region Processes

            private void GetCpuUsage(int id, string processName)
            {
                var perfCounter = new PerformanceCounter("Process", "% Processor Time", processName);
                float cpu = 0;

                var cpuThread = CpuThreadList.First(x => x.Id == id);

                try
                {
                    
                    perfCounter.NextValue();

                    for (int i = 0; i < 3; i++)
                    {
                      
                        Thread.Sleep(5);
                        cpu = perfCounter.NextValue() / Environment.ProcessorCount;
                    }
                }
                catch { }
                cpuThread.CpuUsage = cpu;
            }

            private void GetProcessList(string updator)
            {
                dgProcesses.Items.Clear();
                CpuThreadList = new List<CpuThread>();
                DateTime timeStart = DateTime.Now;

                var processes = Process.GetProcesses().GroupBy(g => g.ProcessName);
                foreach (var pg in processes)
                {
                    if (pg.Count() == 1)
                    {
                        try
                        {
                            dgProcesses.Items.Add(new MyData
                            {
                                ProcessName = pg.First().ProcessName,
                                Id = pg.First().Id.ToString(),
                                Memory = (pg.First().VirtualMemorySize64 / 1024 / 1024 / Environment.ProcessorCount).ToString() + "K",
                                IsResponding = pg.First().Responding.ToString(),

                                // CpuUsage оновимо, коли порахуємо
                                CpuUsage = "",

                                StartDate = pg.First().StartTime.ToString("yyyy-MM-dd"),
                                StartTime = pg.First().StartTime.ToString("HH:mm:ss.fff"),
                                MainModuleFileName = pg.First().MainModule.FileName,
                                UserName = pg.First().StartInfo.UserName,

                                  NumericMemory = (pg.First().VirtualMemorySize64 / 1024 / 1024),

                               
                                // DoubleCpuUsage оновимо, коли порахуємо
                                DoubleCpuUsage = 0
                            });

                            // Для кожного Process запускаемо окремий поток, щоб распараллелити обробку
                            
                            CpuThreadList.Add(new CpuThread { CpuUsage = -1, Id = pg.First().Id });

                            Thread t = new Thread(() => GetCpuUsage(pg.First().Id, pg.First().ProcessName));
                            t.Start();
                        }
                        catch { } 
                    }

                    else
                    {
                        int id = 1;
                        foreach (var p in pg)
                        {
                            try
                            {
                                dgProcesses.Items.Add(new MyData
                                {
                                    ProcessName = p.ProcessName,
                                    Id = p.Id.ToString(),
                                    Memory = (p.VirtualMemorySize64 / 1024 / 1024 / Environment.ProcessorCount).ToString() + "K",
                                    IsResponding = p.Responding.ToString(),

                                   
                                    CpuUsage = "",

                                    StartDate = p.StartTime.ToString("yyyy-MM-dd"),
                                    StartTime = p.StartTime.ToString("HH:mm:ss.fff"),
                                    MainModuleFileName = p.MainModule.FileName,
                                    UserName = p.StartInfo.UserName,

                                      NumericMemory = (p.VirtualMemorySize64 / 1024 / 1024),

                                     DoubleCpuUsage = 0
                                });

                            
                                CpuThreadList.Add(new CpuThread { CpuUsage = -1, Id = p.Id });

                                Thread t = new Thread(() => GetCpuUsage(p.Id, p.ProcessName + "#" + id));
                                t.Start();
                            }
                            catch { } 

                            id++;
                        }
                    }
                }

                // Чекаємо поки всі потоки не завершаться
                while (CpuThreadList.Where(x => x.CpuUsage == -1).Any())
                {
                    
                    Thread.Sleep(20);
                }
                // Кінець
                DateTime timeEnd = DateTime.Now;
                TimeSpan ts = timeEnd - timeStart;

                for (int itemIndex = 0; itemIndex < dgProcesses.Items.Count; itemIndex++)
                {
                    MyData item = (MyData)dgProcesses.Items[itemIndex];
                    double cpuUsage = CpuThreadList.Where(x => x.Id.ToString() == item.Id).Select(x => x).Select(x => x.CpuUsage).FirstOrDefault();
                    item.CpuUsage = cpuUsage.ToString("0.00") + " %";
                    item.DoubleCpuUsage = cpuUsage;
                }


                txtProcessCount.Text = string.Format("Кіль.Процесів: {0} ( Updated at {1} by {2} - {3} s.)", dgProcesses.Items.Count.ToString(), DateTime.Now.ToString("HH:mm:ss"), updator, (ts.TotalMilliseconds / 1000).ToString("0.00"));
                dgThreads.Items.Clear();
                dgModules.Items.Clear();
                SetVisibility(Visibility.Hidden);
            }
            private void GetModuleList()
            {
                Process p = GetSelectedProcess();
                if (p != null)
                {
                    dgModules.Items.Clear();
                    dgThreads.Items.Clear();
                    txtModuleCount.Text = "";
                    txtThreadCount.Text = "";

                    if (p != null)
                    {
                          dgModules.Items.Clear();

                        foreach (ProcessModule m in p.Modules)
                        {
                            dgModules.Items.Add(new MyData
                            {
                                Id = m.BaseAddress.ToString(),

                                ModuleName = m.ModuleName,
                                MainModuleFileName = p.MainModule.FileName,
                            });
                        }
                        txtModuleCount.Text = "Module Count: " + p.Modules.Count.ToString();

                        //продивитись список потоків запущених процесом (Ид потоку, стан потоку, дата та час запуску потоку)
                        
                        foreach (ProcessThread t in p.Threads)
                        {
                            try
                            {
                                dgThreads.Items.Add(new MyData
                                {
                                    Id = t.Id.ToString(),
                                    State = t.ThreadState.ToString(),
                                    StartDate = t.StartTime.ToString("yyyy-MM-dd"),
                                    StartTime = t.StartTime.ToString("HH:mm:ss.fff")
                                });
                            }
                            catch { }
                        }
                        txtThreadCount.Text = "Thread Count: " + p.Threads.Count.ToString();
                    }
                }
            }
            private Process GetSelectedProcess()
            {
                return Process.GetProcesses().Where(x => x.Id.ToString() == selectedProcessId).Select(y => y).FirstOrDefault();
            }
            private void ProcessSetSelectedRow()
            {
                if (!IsProcessSelected)
                    return;

                // Отримаємо index обраного item (Process):
                int index = -1;
                foreach (MyData myItem in dgProcesses.Items)
                {
                    index++;
                    if (myItem.Id == selectedProcessId) { break; }
                }
               
                // Після сортування індекси змінилися, теперь index нашого item вже інший.
                // Ми повинні почистити стару строку (та item) і встановити заново.

                dgProcesses.SelectedItems.Clear();

                // Отримуємо наш item вже з новым index:
                object item = dgProcesses.Items[index];

                
                dgProcesses.SelectedItem = item;

                // Знаходимо Grid Row, в якому знаходиться наш item:
                DataGridRow row = (DataGridRow)dgProcesses.ItemContainerGenerator.ContainerFromIndex(index);

                if (row == null)
                {
                   
                   
                    dgProcesses.ScrollIntoView(item);
                   
                    row = dgProcesses.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
                    
                    dgProcesses.UpdateLayout();
                }

                
                row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
            private void ProcessClearSelectedRow()
            {
                // Після того, як ми зупинили Process, очистимо SelectedItem,
                // так як він вже не існує 
                dgProcesses.SelectedItems.Clear();
                dgProcesses.UpdateLayout();
                selectedProcessId = null;
                btnKill.IsEnabled = IsProcessSelected;
                btnOpenFolder.IsEnabled = IsProcessSelected;
            }
            private void dgProcesses_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
               

                var selectedItem = ((DataGrid)sender).SelectedItem;
                if (selectedItem != null)
                {
                    selectedProcessId = ((MyData)selectedItem).Id;
                }

               
                GetModuleList();

                
                SetVisibility(Visibility.Visible);
            }
            private void dgProcesses_Sorting(object sender, DataGridSortingEventArgs e)
            {
                         isProcessSortRequired = true;
            processSortDirection = ListSortDirection.Ascending;

                if (e.Column.SortDirection.HasValue && e.Column.SortDirection.Value == ListSortDirection.Ascending)
                {
                    processSortDirection = ListSortDirection.Descending;
                }

                
                switch (e.Column.SortMemberPath)
                {
                    case "ProcessName":
                        processSortByColumnIndex = 0;
                        break;

                    case "Id":
                        processSortByColumnIndex = 1;
                        break;

                    case "IsResponding":
                        processSortByColumnIndex = 2;
                        break;

                    case "DoubleCpuUsage":
                        processSortByColumnIndex = 3;
                        break;

                    case "NumericMemory":
                        processSortByColumnIndex = 4;
                        break;

                    case "StartDate":
                        processSortByColumnIndex = 5;
                        break;

                    case "StartTime":
                        processSortByColumnIndex = 6;
                        break;

                    case "MainModuleFileName":
                        processSortByColumnIndex = 7;
                        break;

                    case "UserName":
                        processSortByColumnIndex = 8;
                        break;

                    default:
                        processSortByColumnIndex = 0;
                        break;
                }

                
                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    ProcessSetSelectedRow();
                }, null);
            }

            #endregion

            #region Modules
            private void ModuleSetSelectedRow()
            {
                if (!IsModuleSelected)
                    return;

                
                int index = -1;
                foreach (MyData myItem in dgModules.Items)
                {
                    index++;
                    if (myItem.Id == selectedModuleId) { break; }
                }

                
                dgModules.SelectedItems.Clear();

               
                object item = dgModules.Items[index];

               
                dgModules.SelectedItem = item;

                
                DataGridRow row = (DataGridRow)dgModules.ItemContainerGenerator.ContainerFromIndex(index);

                if (row == null)
                {
                       dgModules.ScrollIntoView(item);
                    
                    row = dgModules.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
                    
                    dgModules.UpdateLayout();
                }

                
                row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
            private void dgModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
               

                var selectedItem = ((DataGrid)sender).SelectedItem;
                if (selectedItem != null)
                {
                    selectedModuleId = ((MyData)selectedItem).Id;
                }
            }
            private void dgModules_Sorting(object sender, DataGridSortingEventArgs e)
            {
                

                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    ModuleSetSelectedRow();
                }, null);
            }
            #endregion
        }
    }
