using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading; /* Thread.Sleep(..) */
using System.Diagnostics; /* Debug */

namespace Tdoa_Test
{
    public partial class Form1 : Form
    {
        private ThreadStart childref;
        private Thread childThread;

        public Form1()
        {
            InitializeComponent();
        }

        private UInt64 CalculateUnit(double DistanceInCentimeter)
        {
            double UnitInCentimeter = 4.9450920255e-2;

            return (UInt64)Math.Round(DistanceInCentimeter / UnitInCentimeter);
        }		

        private void Form1_Load(object sender, EventArgs e)
        {
            //.. 8. Create  thread
            childref = new ThreadStart(MyThread);
            childThread = new Thread(childref);
            childThread.Start();

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (childThread != null) {
                childThread.Abort();
            }
        }

        public void MyThread()
        {

            try {
                Debug.WriteLine("Child thread started");

                Trilateration m_Tri = new Trilateration(100);

				/*for (int i= 0;i < 1000; i++) {
                    m_Tri.AnchorAdd((UInt16)(105+i), 0, 0, 0);
                    m_Tri.FeedTdoa(16001, 105, 0);
                }*/
				
				m_Tri.AnchorAdd(100, 0, 0, 0);
				m_Tri.AnchorAdd(101, 2000, 0, 2000);
				m_Tri.AnchorAdd(102, 2000, 2000, 0);
				m_Tri.AnchorAdd(103, 0, 2000, 2000);
				m_Tri.AnchorAdd(104, 0, 0, 2000);

				//double x = -2000, y = 1000, z = 1000; /* Errornous */
				//double x = -2000, y = 1000, z = 200;
				double x = -2000, y = 1000, z = 0;

				Random rnd = new Random();

                //int ErrorMax = 100; /* Hata ya tepkileri çok yüksek !!??*/

                UInt16 TagId = 16000;

				UInt32 SessionId = 0;

				UInt32 LoopCnt = 0;

                while(true){
                     Debug.WriteLine("Feed Tag,Session,x,y,z :{0},{1}: {2},{3},{4}", TagId, SessionId, Math.Round(x), Math.Round(y), Math.Round(z));

					//.. 2D' de z = 0 sürüyoruz
					UInt64 d100 = CalculateUnit(m_Tri.AnchorCalculateDistance(100, x, y, z)/*  + (double)rnd.Next(ErrorMax)*/ );
					UInt64 d101 = CalculateUnit(m_Tri.AnchorCalculateDistance(101, x, y, z)/*  + (double)rnd.Next(ErrorMax)*/ );
					UInt64 d102 = CalculateUnit(m_Tri.AnchorCalculateDistance(102, x, y, z)/*  + (double)rnd.Next(ErrorMax)*/ );
					UInt64 d103 = CalculateUnit(m_Tri.AnchorCalculateDistance(103, x, y, z)/*  + (double)rnd.Next(ErrorMax)*/ );
					UInt64 d104 = CalculateUnit(m_Tri.AnchorCalculateDistance(104, x, y, z)/*  + (double)rnd.Next(ErrorMax)*/ );				

					m_Tri.FeedTdoa(TagId, 100, SessionId, d100);
                    m_Tri.FeedTdoa(TagId, 101, SessionId, d101);
                    m_Tri.FeedTdoa(TagId, 102, SessionId, d102);
                    m_Tri.FeedTdoa(TagId, 103, SessionId, d103);
                    m_Tri.FeedTdoa(TagId, 104, SessionId, d104);

					SessionId++;

					/*Stopwatch sw = new Stopwatch();            
					sw.Reset();
                    sw.Start();
                    UInt32 i;
                    for(i = 0; i < 1000; i++){
                        m_Tri.Trilaterate3D();
                    }
                    sw.Stop();
                    Debug.WriteLine("Elapsed={0}", sw.Elapsed);*/

					if (LoopCnt % 1 == 0) {
						List<TagLoc> LocList = m_Tri.Trilaterate3D();
						foreach (TagLoc tagloc in LocList) {
							foreach(Loc loc in tagloc.m_Locs) {
								Debug.WriteLine("Calc Tag,Session,x,y,z :{0},{1}: {2},{3},{4}", tagloc.Id, loc.SessionId, Math.Round(loc.x), Math.Round(loc.y), Math.Round(loc.z));
							}
						}
					}					

					Debug.WriteLine("\n");

					x += 100; if (x > 2000) x = -1000;

					LoopCnt++;

					//m_Tri.AnchorRemove(104);

					Thread.Sleep(1000);
                }
            }
            catch (ThreadAbortException e){
                Debug.WriteLine("Io thread aborted:{0}", e.Message);
            }
            finally{
                Debug.WriteLine("Couldn't catch the Thread Exception");
                //.. close file               
            }
        }
    }
}
