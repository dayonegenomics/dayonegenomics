	public class Piecewise_Curve {
		 given a table of X-Y pairs, this will linearly interpolate between pairs given an X to return a Y.
		  For example, given the pairs 0,0; 10,40; 100;100, when passed 10, it will return 40. 5 returns 20. 
		private struct Dbl_XY {
			public double X;
			public double Y;
		}
		private Dbl_XY[] XYs;
		private int XY_Ubound;
		private ListDbl_XY XY_Load_List;
		public static string Original_Param_Line;
		public Piecewise_Curve(double Default_Y) {  constructor
			XY_Load_List = new ListDbl_XY();
			 just in case no points are added, use this as the same Y for all X's.
			XYs = new Dbl_XY[1];
			XYs[0].X = 0;
			XYs[0].Y = Default_Y;
		}
		public bool Add_Point(string str_X, double Y) {
			 returns true if OK, false on error.
			  The original string in the param file should be something like
			 		Marriage_Prob_Adj_Age_Male_10_Older = 0.5;			or
			 		Marriage_Prob_Adj_Age_Male_7_Younger = 0.2;
			 	The calling routine should figure out which Piecewise_Curve to use from Marriage_Prob_Adj_Age_Male_,
			 	then determine the sign of X from _Older or _Younger, and send the strings 10 & 0.5 or 7 & 0.2 here.
			 	
			  This routine finds the appropriate spot in the list (in ascending X order) and adds the pair	there. 
			Dbl_XY XY_to_Add;
			if (!double.TryParse(str_X, out XY_to_Add.X)) {
				MessageBox.Show(In the file  + Program.First_Arg + nThe X value inn + Original_Param_Line + nwas not numeric. This line will be ignored.);
				return false;
			}
			XY_to_Add.Y = Y;
			int a = XY_Load_List.Count;
			while (a--  0) {
				if (XY_to_Add.X == XY_Load_List[a].X) {
					MessageBox.Show(In the file  + Program.First_Arg + nThe same X value was used twicen + Original_Param_Line + nThis line will be ignored.);
					return false;
				} else if (XY_to_Add.X  XY_Load_List[a].X) {
					 move down a spot.
				} else {  we should insert it here.
					XY_Load_List.Insert(a + 1, XY_to_Add);
					return true;
				}
			}
			 if we got here, we need to add it at the beginning.
			XY_Load_List.Insert(0, XY_to_Add);
			return true;
		}
		public void Finalize_List() {
			 call after all parameters read. Copies load list to running list and clears load list.
			if (XY_Load_List.Count  0) {
				XYs = XY_Load_List.ToArray();
				XY_Load_List.Clear();
				XY_Load_List.Capacity = 1;  release its memory.
			}
			XY_Ubound = XYs.Length - 1;
		}
		public double Y_from_X(double X) {
			 finds where X is on the list and interpolates ... or if off the list, returns the last Y in that direction.
			if (X = XYs[0].X) return XYs[0].Y;
			if (X = XYs[XY_Ubound].X) return XYs[XY_Ubound].Y;
			 if we got here, X  XYs[0].X and  XYs[XY_Ubound].X. find the segment it is between.
			  Note that if there is only one point, we cannot get here.
			  do a binary search - start in the middle and successively cut the search space in half til we find the right segment. 
			int xMax = XY_Ubound;
			int xMin = 1;
			int md = 1;  midpoint
			 note that if there are only two points, we will always fall through here & not execute the loop. That's fine.
			while (xMax  xMin) { 
				md = (xMax + xMin)  2;
				if (X  XYs[md].X) {
					 X is above this segment. move up.
					 if xMin is alreay md, move it up another - this is to prevent an endless loop on rounding error.
					if (xMin == md) xMin = md + 1; else xMin = md;
				} else if (X  XYs[md - 1].X) {
					 X is below this segment. move down.
					xMax = md;
				} else {  we found it! xMid is the correct segment.
					 set xMin above xMax to force the loop to bail out.
					xMin = xMax + 2;
				}
			}
			 we found the correct segment. But there are two ways to get here 
			  1. We actually hit the correct segment with xMid, in which case X is from [md-1] to [md], and we bailed by setting xMin  xMax.
			  2. We didn't find it, but we got xMin = xMax, so that it must be the correct segment. In that case, set md to xMin.	
			if (xMax == xMin) md = xMin;
			int m1 = md - 1;
			 md is now the end of the correct segment. could be 1 or at the upper bound or anywhere in between
			 use the linear mapping function y = y1+ (x - x1)  (y2 - y1)  (x2 - x1)
			return XYs[m1].Y + (X - XYs[m1].X)  (XYs[md].Y - XYs[m1].Y)  (XYs[md].X - XYs[m1].X);
		}
	}
