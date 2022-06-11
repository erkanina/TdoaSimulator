using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics; /* Debug */
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization; /* Svd */
using System.Globalization; /* formatProvider */

//using System.Collections.Concurrent; /* TODO: Threa safe kullanım için List yerine - bunda da tam güvenlik yok*/

namespace Tdoa_Test
{
	class Loc
	{
		public UInt32 SessionId { get; set; }
		public long TimeLocal { get; set; }
		public double x { get; set; }
		public double y { get; set; }
		public double z { get; set; }

		public Loc(UInt32 rSessionId, long rTimeLocal, double rx, double ry, double rz)
		{
			SessionId = rSessionId;
			TimeLocal = rTimeLocal;
			x = rx;
			y = ry;
			z = rz;
		}
	}

	class TagLoc
    {
		public UInt16 Id { get; set; }
		public List<Loc> m_Locs = null;

        public TagLoc(UInt16 id)
        {
            Id = id;
			m_Locs = new List<Loc>();

		}

		public void AddLoc(UInt32 SessionId, long TimeLocal, double x, double y, double z)
		{			
			m_Locs.Add(new Loc(SessionId, TimeLocal, x, y, z));
		}
	}

    //------------------------------------------------------------------

    class Anchor
    {       
        public UInt16 Id { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }

        public Anchor(UInt16 id, double rx, double ry, double rz)
        {
            Id = id;
            x = rx;
            y = ry;
            z = rz;
        }
    }

	//------------------------------------------------------------------

	class Tdoa
	{
		public Anchor Anchor { get; set; }
		public UInt64 Time { get; set; }
		public long TimeLocal { get; set; }

		public Tdoa(Anchor rAnchor, UInt64 rTime, long rTimeLocal)
		{
			Anchor = rAnchor;
			Time = rTime;
			TimeLocal = rTimeLocal;
		}
	}

	class Session
	{
		public UInt32 Id { get; set; }
		public long TimeLocal { get; set; }
		public bool blProcessed = false;
		public List<Tdoa> m_Tdoas = null;

		public Session(UInt32 SessionId)
		{
			Id = SessionId;

			m_Tdoas = new List<Tdoa>();			
		}

		public void FeedTdoa(Anchor Anchor, UInt64 Time)
		{
			long now = DateTime.Now.Ticks;

			Tdoa tdoa = m_Tdoas.Find(item => item.Anchor == Anchor);
			if(tdoa == null) {
				tdoa = new Tdoa(Anchor, Time, now);

				m_Tdoas.Add(tdoa);
			}
			else {
				/* overwrite if exist, nonsense, ihtiyaten */
				tdoa.Time = Time;
				tdoa.TimeLocal = now;
			}

			TimeLocal = now; //.. parente de koydum hızlı tarama yapmak için
		}		
	}

	//------------------------------------------------------------------

	class Tag
    {
        private Trilateration root;

        public UInt16 Id { get; set; }
        
		public List<Session> m_Sessions = null;

		private const int MIN_SAMPLE_FOR_3D = 4;
        private const int MAX_SAMPLE_FOR_3D = 12;

        private const int MIN_SAMPLE_FOR_2D = 3;
        private const int MAX_SAMPLE_FOR_2D = 12;

        private const double DWT_TIME_UNITS = 15.65; /* 15.65 psec = 15.65e-12 sec */
        
        public Tag(Trilateration rroot, UInt16 id)
        {
            root = rroot;
            Id = id;
						
			m_Sessions = new List<Session>();
		}        

        public void FeedTdoa(UInt16 AnchorId, UInt32 SessionId, UInt64 Time)
        {
			Session session = m_Sessions.Find(item => item.Id == SessionId);
			if(session == null) {
				session = new Session(SessionId);
				m_Sessions.Insert(0, session);
			}

			//.. Root'daki Anchor'u bul
			Anchor Anchor = root.m_Anchors.Find(item => item.Id == AnchorId);
			if (Anchor == null) {
				return; // Olmayan Anchor'lari işlemiyoruz
			}

			session.FeedTdoa(Anchor, Time);			

			while (m_Sessions.Count > 5) {
				m_Sessions.RemoveAt(m_Sessions.Count - 1);
			}
		}        

        /*private UInt64 TickElapsed(UInt64 cur, UInt64 prev)
        {
            UInt64 elapsed;
            if (cur >= prev) elapsed = cur - prev;
            else elapsed = (UInt64.MaxValue - prev) + cur;            
            return elapsed;          
        }*/

        /*public static T Max<T>(T x, T y)
        {
            return (Comparer<T>.Default.Compare(x, y) > 0) ? x : y;
        }

        public static T Min<T>(T x, T y)
        {
            return (Comparer<T>.Default.Compare(x, y) > 0) ? y : x;
        }*/

        private Int64 UnitDiff(Int64 cur, Int64 prev)
        {
            Int64 diff1 = cur - prev;
            Int64 diff2 = prev - cur;

            if (Math.Abs(diff1) <= Math.Abs(diff2)){
                return diff1;
            }
            else{
                return diff2;
            }          
        }

		public Matrix<double> SingulerValueDecomposition(Matrix<double> A, Matrix<double> b)
		{
			/*
			Svd<double> svd = A.Svd(true);
			double ConditionMumber = svd.ConditionNumber;			
			Debug.WriteLine("Cond: {0}", svd.ConditionNumber);

			if((svd.ConditionNumber == double.PositiveInfinity) || (svd.ConditionNumber == double.NegativeInfinity)) {
				Debug.WriteLine("Condition number is infinity");
				return null;
			}
			
			// get matrix of left singular vectors with first n columns of U			
			Matrix<double> U1 = svd.U.SubMatrix(0, A.RowCount, 0, A.ColumnCount);

			// get matrix of singular values
			Matrix<double> S = new DiagonalMatrix(A.ColumnCount, A.ColumnCount, svd.S.ToArray());

			// get matrix of right singular vectors
			Matrix<double> V = svd.VT.Transpose();

			return V.Multiply(S.Inverse()).Multiply(U1.Transpose().Multiply(b));
			*/

			Svd<double> svd = A.Svd(true);
			double ConditionMumber = svd.ConditionNumber;
			Debug.WriteLine("Rank, Det, Cond: {0},{1},{2}", svd.Rank, svd.Determinant, svd.ConditionNumber);
			

			if ((svd.ConditionNumber == double.PositiveInfinity) || (svd.ConditionNumber == double.NegativeInfinity)) {
				Debug.WriteLine("Condition number is infinity");
				return null;
			}

			return svd.Solve(b);
		}

		public Matrix<double> LeastSquare(Matrix<double> A, Matrix<double> b)
		{
			//Svd<double> svd = A.Svd(true);
			//Debug.WriteLine("Cond: {0}\r", svd.ConditionNumber);

			//.. Linear Least Square Regression			
			Matrix <double> tA = A.Transpose();			

			Matrix<double> tAxA = tA.Multiply(A);

			if (tAxA.Determinant() == 0) {
				Debug.WriteLine("Matrice is Not invertible");
				return null;
			}

			Matrix<double> itAxA = tAxA.Inverse();			

			Matrix<double> itAxAxtA = itAxA.Multiply(tA);

			Matrix<double> Xls = itAxAxtA.Multiply(b);

			return Xls;

			//return A.Transpose().Multiply(A).Inverse().Multiply(A.Transpose().Multiply(b));
			//return A.QR().Solve(b); /* QR decomposition */
			//return A.Svd(true).Solve(b);
			/* http://www.imagingshop.com/linear-and-nonlinear-least-squares-with-math-net/ */
		}

		public Matrix<double> QRDecomposition(Matrix<double> A, Matrix<double> b)
		{
			return A.QR().Solve(b); /* QR decomposition */
		}

		public TagLoc Trilaterate2D()
        {			
			TagLoc tloc = new TagLoc(Id);

			//.. 1. Session'u local time'a göre büyükten küçüğe sırala
			m_Sessions.Sort((x, y) => -1 * x.TimeLocal.CompareTo(y.TimeLocal)); //.. büyükten küçüğe sıralar, en yenisi ilk başta

			//.. 2. En yeniden en eskiye session'daki Anchor slot sayısı yeterli sayıdaysa
			for(int s=0;s< m_Sessions.Count;s++) {
				Session  session = m_Sessions.ElementAt(s);

				if(session.blProcessed) continue;

				//Debug.WriteLine("Session: {0},{1}", session.Id, session.m_Tdoas.Count);

				if (session.m_Tdoas.Count >= MIN_SAMPLE_FOR_2D) {

					Matrix<double> A = new DenseMatrix(session.m_Tdoas.Count - 1, 3);
					Matrix<double> b = new DenseMatrix(session.m_Tdoas.Count - 1, 1);
					
					Tdoa a0 = session.m_Tdoas.ElementAt(0);

					for (int i=0;i< A.RowCount;i++) {
						Tdoa ai = session.m_Tdoas.ElementAt(i + 1);

						A[i, 0] = a0.Anchor.x - ai.Anchor.x;
						A[i, 1] = a0.Anchor.y - ai.Anchor.y;
						A[i, 2] = ((double)UnitDiff((Int64)a0.Time, (Int64)ai.Time)) * root.UnitInCentimeter;

						b[i, 0] = 0.5 * (Math.Pow(a0.Anchor.x, 2) - Math.Pow(ai.Anchor.x, 2) +
										 Math.Pow(a0.Anchor.y, 2) - Math.Pow(ai.Anchor.y, 2) +
										 Math.Pow(a0.Anchor.z, 2) - Math.Pow(ai.Anchor.z, 2) +
										 Math.Pow(A[i, 2], 2));
					}

					/*Matrix<double> Xls = LeastSquare(A, b);
					if (Xls != null) {
						tloc.AddLoc(session.Id, session.TimeLocal, Xls[0, 0], Xls[1, 0], 0);
					}*/

					
					Matrix<double> Svd = SingulerValueDecomposition(A, b);
					if (Svd != null) {
						tloc.AddLoc(session.Id, session.TimeLocal, Svd[0, 0], Svd[1, 0], 0);
					}

					/*Matrix<double> Qrd = QRDecomposition(A, b);
					if (Qrd != null) {
						tloc.AddLoc(session.Id, session.TimeLocal, Qrd[0, 0], Qrd[1, 0], 0);
					}*/

					session.blProcessed = true;
				}
			}

			return tloc;
		}

        public TagLoc Trilaterate3D()
        {			
			TagLoc tloc = new TagLoc(Id);

			//.. 1. Session'u local time'a göre büyükten küçüğe sırala
			m_Sessions.Sort((x, y) => -1 * x.TimeLocal.CompareTo(y.TimeLocal)); //.. büyükten küçüğe sıralar

			//.. 2. En yeniden en eskiye session'daki Anchor slot sayısı yeterli sayıdaysa
			for (int s = 0; s < m_Sessions.Count; s++) {
				Session session = m_Sessions.ElementAt(s);

				if (session.blProcessed) continue;

				if (session.m_Tdoas.Count >= MIN_SAMPLE_FOR_2D) {

					Matrix<double> A = new DenseMatrix(session.m_Tdoas.Count - 1, 4);
					Matrix<double> b = new DenseMatrix(session.m_Tdoas.Count - 1, 1);

					UInt32 SessionId = session.Id;
					Tdoa a0 = session.m_Tdoas.ElementAt(0);

					for (int i = 0; i < A.RowCount; i++) {
						Tdoa ai = session.m_Tdoas.ElementAt(i + 1);

						A[i, 0] = a0.Anchor.x - ai.Anchor.x;
						A[i, 1] = a0.Anchor.y - ai.Anchor.y;
						A[i, 2] = a0.Anchor.z - ai.Anchor.z;
						A[i, 3] = ((double)UnitDiff((Int64)a0.Time, (Int64)ai.Time)) * root.UnitInCentimeter;

						b[i, 0] = 0.5 * (Math.Pow(a0.Anchor.x, 2) - Math.Pow(ai.Anchor.x, 2) +
										 Math.Pow(a0.Anchor.y, 2) - Math.Pow(ai.Anchor.y, 2) +
										 Math.Pow(a0.Anchor.z, 2) - Math.Pow(ai.Anchor.z, 2) +
										 Math.Pow(A[i, 3], 2));


					}

					/*Matrix<double> Xls = LeastSquare(A, b);
					if (Xls != null) {
						tloc.AddLoc(SessionId, session.TimeLocal, Xls[0, 0], Xls[1, 0], Xls[2, 0]);
					}*/

					Matrix<double> Svd = SingulerValueDecomposition(A, b);
					if (Svd != null) {
						tloc.AddLoc(SessionId, session.TimeLocal, Svd[0, 0], Svd[1, 0], Svd[2, 0]);
					}

					/*Matrix<double> Qrd = QRDecomposition(A, b);
					if (Qrd != null) {
						tloc.AddLoc(SessionId, session.TimeLocal, Qrd[0, 0], Qrd[1, 0], Qrd[2, 0]);
					}*/

					session.blProcessed = true;
				}
			}

			return tloc;
		}
    }

    //------------------------------------------------------------------    

    class Trilateration
    {
		List<Tag> m_Tags { get; set; }
        public List<Anchor> m_Anchors { get; set; }

        //public const double SpeedOfLightInAir = 299702547.0; /* meter per second */
        public double UnitInCentimeter = 4.9450920255e-2;

        public long MaxInterval = 100; //3000; /* in miliseconds */

        public void DumpMatrix(Matrix<double> M, string Title)
        {
            var formatProvider = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            formatProvider.TextInfo.ListSeparator = " ";

            Console.WriteLine(Title);
            Console.WriteLine(M.ToString("#0.000000\t", formatProvider));
            Console.WriteLine();
        }       

        public Trilateration(long rMaxInterval)
        {
            m_Tags = new List<Tag>();
            m_Anchors = new List<Anchor>();

            MaxInterval = rMaxInterval;
        }

        public void AnchorAdd(UInt16 id, double x, double y, double z)
        {
            Anchor found = m_Anchors.Find(item => item.Id == id);
            if (found == null)  {
                m_Anchors.Add(new Anchor(id, x, y, z));
            }
        }

        public double AnchorCalculateDistance(UInt16 id, double x, double y, double z)
        {
            Anchor found = m_Anchors.Find(item => item.Id == id);
            if (found != null) {
                return (Math.Pow(Math.Pow(found.x - x, 2) + Math.Pow(found.y - y, 2) + Math.Pow(found.z - z, 2), 0.5));               
            }

            return 0;
        }

        /*public void AnchorRemove(UInt16 id)
        {
			
            //.. Tag'lardaki entry'leri sil
            foreach (Tag tag in m_Tags) {
                Tdoa tdoa = tag.m_Tdoas.SingleOrDefault(r => r.Anchor.Id == id);
                if (tdoa != null) {
                    tag.m_Tdoas.Remove(tdoa);
                }
            }

            //.. Kökteki anchor'u remove et
            Anchor found = m_Anchors.SingleOrDefault(r => r.Id == id);
            if (found != null) {
                m_Anchors.Remove(found);
            }
        }*/

		public void AnchorRemove(UInt16 Id)
		{
			//.. 1. Tag'lardaki entry'leri sil
			foreach(Tag tag in m_Tags) {
				foreach(Session session in tag.m_Sessions) {					
					for(int i = session.m_Tdoas.Count - 1; i>= 0; i--) {
						if(session.m_Tdoas.ElementAt(i).Anchor.Id == Id) {
							session.m_Tdoas.RemoveAt(i);
						}
					}
				}
			}

			//.. 2. Root'daki entry'yi sil
			for (int i = m_Anchors.Count-1;i >= 0; i--) {
				if(m_Anchors.ElementAt(i).Id == Id) {
					m_Anchors.RemoveAt(i);
				}			
			}
		}

		public void FeedTdoa(UInt16 TagId, UInt16 AnchorId, UInt32 SessionId, UInt64 time)
        {
			Tag tag = m_Tags.Find(item => item.Id == TagId);
            if (tag == null) {
                tag = new Tag(this, TagId);
                m_Tags.Add(tag);
            }

            tag.FeedTdoa(AnchorId, SessionId, time);
        }       

        public List<TagLoc> Trilaterate2D()
        {
            List<TagLoc> LocList = new List<TagLoc>();
            foreach (Tag tag in m_Tags)  {
                TagLoc loc = tag.Trilaterate2D();
                if (loc != null)  {
                    LocList.Add(loc);
                }
            }

            return LocList;
        }

        public List<TagLoc> Trilaterate3D()
        {
            List<TagLoc> LocList = new List<TagLoc>();
            foreach (Tag tag in m_Tags)  {
                TagLoc loc = tag.Trilaterate3D();
                if (loc != null)  {
                    LocList.Add(loc);
                }
            }

            return LocList;			
        }
    }
}
