#region "Introduction"
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Linq;
/* Programming philosophy:
 *		Because the pieces of this program are not planned for use by others, because nobody is depending on it
 *		to work correctly all the time, and because speed is of the essence, I have deliberately chosen not to
 *		use much crash protection. A program crash usually means that the simulation has done something highly
 *		unrealistic (for example, having someone with a negative age) which means that if we trapped the bug and
 *		kept going, the result of the run will be garbage, and we will not know there was a bug unless we review
 *		the diagnostic files.
 *	
 * This program is the brainchild of Dr. Robert Carter, and was coded by Chris Hardy in June 2014.  */
#endregion
namespace PopGen2 {
	public partial class frm_PopGen : Form {
		private string Original_Text;
		private DateTime PauseTime;
		public frm_PopGen() {
			InitializeComponent();
		}
		private void frm_PopGen_Load(object sender, EventArgs e) {
			cbx_Diag_Trace.Checked = false; // set true to turn on debug trace from the beginning.
			cbx_Slow_Step.Checked = false;
			Globals.Re_Init();
			Original_Text = Text + " - " + Globals.Parameter_File_Name;
			ProgBar.Maximum = (int)Globals.Total_Samples;
			ProgBar.Minimum = 0;
			tmr_Iterate.Enabled = true;
		}
		private void Update_Labels() {
			lbl_Date.Text = "Date: " + Globals.Iter_Date.ToString(Globals.DateFormat) + "    Sample " + Globals.Permutation_Sample_Num.ToString() + "    Min CBA " + Globals.Fema.Min_CBA.ToString() + "    Interval " + Globals.Min_Gap_Between_Children.ToString(Globals.DateFormat);
			lbl_Girls.Text = Globals.Girls.Count.ToString("#,0") + " Girls";
			lbl_Boys.Text = Globals.Boys.Count.ToString("#,0") + " Boys";
			lbl_Ladies_Available.Text = Globals.Available_Ladies.Count.ToString("#,0") + " Available Ladies";
			lbl_Wives.Text = Globals.Wives_Count.ToString("#,0") + " Wives";
			lbl_Men.Text = Globals.Households.Count.ToString("#,0") + " Men";
			lbl_Widows_PCBA.Text = Globals.Widows_PCBA.Count.ToString("#,0") + " Widows PCBA";
			lbl_Total_Pop.Text = Globals.Total_Pop.ToString("#,0") + " Total Pop";

			ProgBar.Value = Globals.Samples_Run;
			Text = Globals.str_PctDone + Original_Text;
			lbl_Progress.Text = Globals.str_ElapsedTime + Globals.str_Progress;
		}
		private void tmr_Iterate_Tick(object sender, EventArgs e) {
			if (Globals.Already_Executing) return;

			Globals.Execute_Iterations();
			Update_Labels();
			if (!Globals.Still_Running) tmr_Iterate.Enabled = false;
			if (Globals.Pause_After_Run > 1) {
				Pause_or_Resume();
				Globals.Pause_After_Run = 0;
				btn_Pause_After_Run.Enabled = true;
			}
		}
		private void btn_Publish_Click(object sender, EventArgs e) {
			// request to write the most recently prepared results to file again.
			Globals.Publish_Results_to_Files();
		}
		private void ckb_Slow_Step_CheckedChanged(object sender, EventArgs e) {
			Globals.Iterate_Slowly = cbx_Slow_Step.Checked;
		}
		private void btn_Pause_Resume_Click(object sender, EventArgs e) {
			Pause_or_Resume();
		}
		private void Pause_or_Resume() {
			tmr_Iterate.Enabled = !tmr_Iterate.Enabled;
			if (tmr_Iterate.Enabled) {
				btn_Pause_Resume.Text = "Pause";
				Globals.StartTime = Globals.StartTime.Add(DateTime.Now.Subtract(PauseTime));
			} else {
				btn_Pause_Resume.Text = "Resume";
				PauseTime = DateTime.Now;
			}
		}
		private void txb_Timer_MS_TextChanged(object sender, EventArgs e) {
			int i;
			if (int.TryParse(txb_Timer_MS.Text, out i)) {
				if (i > 0 && i < 60000) tmr_Iterate.Interval = i;
			}
		}
		private void btn_Pause_After_Run_Click(object sender, EventArgs e) {
			Globals.Pause_After_Run = 1;
			btn_Pause_After_Run.Enabled = false;
		}
		private void cbx_Diag_Trace_CheckedChanged(object sender, EventArgs e) {
			Globals.Output_Debug_Trace = cbx_Diag_Trace.Checked;
		}
	}
	#region "structures"
	public struct struct_Actuarial_Table {
		public double PerYear;
		public double PerIter;
	}
	public struct struct_Actuarial_Point {
		public int Age;
		public double Rate;
	}
	public struct struct_Info_by_Sex {
		public struct_Actuarial_Table[] ActuarialTable;
		public double Max_Age;
		public double Min_CBA;
		public double Max_CBA;
		public double Mortality_while_0;
		public List<struct_Actuarial_Point> Mortality_Point;
		public int[] Child_Count; // tells how many people of this sex died having the indexed number of children.
		public void Init() {
			// initialize all constants which may differ between males and females.
			int i = 0;
			int xMP = 0; // mortality point index
			int iMaxAge = (int)Max_Age;
			double ActTablMult;
			double Mort_Rate_From;
			double Mort_Rate_To;
			int Mort_Age_From;
			int Mort_Age_To;
			ActuarialTable = new struct_Actuarial_Table[5 + Globals.Max_Either_Age];

			/*		Actuarial tables are adjusted to per-scan like the above probabilities.
			 *		Therefore, they will show the probability of dying per scan given the person's age and gender
			 *		(there is a table for males and a separate table for females). The table will start with a
			 *		moderately high infant mortality and then should drop down to a low 2nd year mortality.
			 *		Then we fit an exponential curve between a series of age-rate points in the parameter file.
			 *		Generally it should drop to age 10, then have an exponential rise to the maximum age, where
			 *		the probability of dying (for this program) reaches 1 to prevent overflowing.   */
			ActuarialTable[0].PerYear = Mortality_while_0;
			Mort_Rate_To = Mortality_while_0;
			Mort_Age_To = 0;
			while (xMP <= Mortality_Point.Count) {
				Mort_Rate_From = Mort_Rate_To;
				Mort_Age_From = Mort_Age_To;
				if (xMP < Mortality_Point.Count) {
					// get the next point from the list of points.
					Mort_Rate_To = Mortality_Point[xMP].Rate;
					Mort_Age_To = Mortality_Point[xMP].Age;
				} else { // we're past the end. the last point is a rate of 1 at the maximum age.
					Mort_Rate_To = 1;
					Mort_Age_To = iMaxAge;
				}
				// mortality rate decreases on an exponential decay curve from 2 to 10 (or what Mortality_Min_Age is set to) years old.
				// calculate a multiplier where we can start with the age 2 rate & end up at the minimum age rate at Mortality_Min_Age:
				ActTablMult = Math.Pow(Mort_Rate_To / Mort_Rate_From, 1 / (double)(Mort_Age_To - Mort_Age_From));
				while (++i < Mort_Age_To) {
					// run through the years in this segment and calculate the rate for each year.
					ActuarialTable[i].PerYear = ActuarialTable[i - 1].PerYear * ActTablMult;
				}
				ActuarialTable[i].PerYear = Mort_Rate_To;
				xMP++;
			}
			/*		Go back through and calculate the per-scan mortality rate so that it's equivalent to the annual.
			 *		Example: a 72 year old woman living in the USA had a 20% chance of dying in 2009 (0.2 mortality).
			 *		If we scan 10 times per year, a probability of 0.02207 (2.207%) each time would result in a 20%
			 *		annual probability of dying that year.
			 *		We want to create the per-iteration table now (at the beginning) so we can just look it up without
			 *		having to do any calculation beyond converting the age to an integer to use as the array index. 	*/
			for (i = 0; i < iMaxAge; i++) {
				ActuarialTable[i].PerIter = Globals.Probability_PerIter_from_PerYear(ActuarialTable[i].PerYear);
			}
			// finally, make sure they're really good & dead at and past the maximum age.
			for (i = Globals.Max_Either_Age + 4; i >= iMaxAge; i--) {
				ActuarialTable[i].PerYear = 1;
				ActuarialTable[i].PerIter = 1.000000001;
			}
		}
		public void Add_Mortality_Point(int Age, double Rate) {
			if (Rate > 0 && Rate < 1) {
				struct_Actuarial_Point ActPt;
				ActPt.Age = Age;
				ActPt.Rate = Rate;
				Mortality_Point.Add(ActPt);
			} else {
				MessageBox.Show("In the file " + Program.First_Arg +  "\nThe parameter " + Globals.ParamLine + " had an invalid mortality rate " + Rate.ToString() + "\nIt must be >0 and <1.");
			}
		}
	}
	public struct struct_Person {
		// this struct is used for individuals and wives
		public double BirthDate;
		public double Min_Next_Child;
		public int Num_Children;
		public int Num_Marriages;
		public int Gens_Removed_Min;
		public double Gens_Removed_Avg;
		public int Gens_Removed_Max;
		private static double dAge;
		private static int iAge;
		public static int xGens_Rem_Dist = 0;
		public void Debug_Trace_Append() {
			Globals.Debug_Trace.Append("," + BirthDate.ToString(Globals.DateFormat));
			Globals.Debug_NumChildren.Append("," + Num_Children.ToString());
			Globals.Debug_MinNextChild.Append("," + Min_Next_Child.ToString(Globals.DateFormat));
			Globals.Debug_NumMarriages.Append("," + Num_Marriages.ToString());
		}
		public void New(double New_BirthDate, int GenRem_Max, double GenRem_Avg, int GenRem_Min) {
			// call this for a new baby.
			BirthDate = New_BirthDate;
			Min_Next_Child = 0;
			Num_Marriages = 0;
			Num_Children = 0;
			Gens_Removed_Max = GenRem_Max + 1;
			Gens_Removed_Avg = GenRem_Avg + 1;
			Gens_Removed_Min = GenRem_Min + 1;
		}
		public void Transfer_From(ref struct_Person Other_Individual) {
			BirthDate = Other_Individual.BirthDate;
			Min_Next_Child = Other_Individual.Min_Next_Child;
			Num_Marriages = Other_Individual.Num_Marriages;
			Num_Children = Other_Individual.Num_Children;
			Gens_Removed_Max = Other_Individual.Gens_Removed_Max;
			Gens_Removed_Avg = Other_Individual.Gens_Removed_Avg;
			Gens_Removed_Min = Other_Individual.Gens_Removed_Min;
		}
		public void Capture_Gens_Rem_Dist() {
			// update the generations-removed distribution for this individual
			Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Gens_Rem_Dist[xGens_Rem_Dist].Add_to_Dist(
				Gens_Removed_Max, Gens_Removed_Avg, Gens_Removed_Min);
		}
		public bool Scan_Wife(ref struct_Person Husband, bool Husband_PCBA) {
			// returns true if the wife died, false if she's OK.
			dAge = Globals.Iter_Date - BirthDate;
			iAge = (int)dAge;

			// see if the wife died of old age / non-childbirth-related causes.
			if (Globals.Fema_Died(iAge)) {
				Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Dead_Wives_by_Age++;
				return true; // she died.
			}

			// see if she or her husband are too old to have children.
			if (Husband_PCBA || dAge > Globals.Fema.Max_CBA) return false; // too old.

			// if we are here, the wife is alive, young enough to have kids, and it's been long enough since marriage and/or the last kid.
			if (Globals.Have_a_Baby(iAge, Min_Next_Child)) {
				// there was a childbirth event - either the mother died, or a kid or two was born.
				if (Globals.Childbirth_Mother_Died) {
					Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Dead_Wives_by_ChildBirth++;
					return true; // wife died during childbirth.
				}

				if (Globals.Childbirth_Boy) { // it's a boy!
					Num_Children++;
					Husband.Num_Children++;
					Globals.Boys.Add(Globals.Iter_Date, Globals.Higher_of(Husband.Gens_Removed_Max, Gens_Removed_Max)
						, (Husband.Gens_Removed_Avg + Gens_Removed_Avg) / 2 ,Globals.Lower_of (Husband.Gens_Removed_Min, Gens_Removed_Min));
				}
				if (Globals.Childbirth_Girl) { // it's a girl!
					Num_Children++;
					Husband.Num_Children++;
					Globals.Girls.Add(Globals.Iter_Date, Globals.Higher_of(Husband.Gens_Removed_Max, Gens_Removed_Max)
						, (Husband.Gens_Removed_Avg + Gens_Removed_Avg) / 2, Globals.Lower_of(Husband.Gens_Removed_Min, Gens_Removed_Min));
				}
				Min_Next_Child = Globals.Iter_Date + Globals.Min_Gap_Between_Children;
			}
			return false;
		}
	}
	public class cls_Distribution {
		public Int64[] Dist;
		public cls_Distribution(int Init_Size) { // constructor
			Dist = new Int64[Init_Size];
		}
		public void Add_to_Dist(int Dist_Level) {
			// expand the levels array if necessary.
			if (Dist_Level >= Dist.Length) Array.Resize<Int64>(ref Dist, Dist_Level + 1);
			Dist[Dist_Level]++;
		}
	}
	public class cls_Gens_Rem_Dist {
		public cls_Distribution Max;
		public cls_Distribution Avg;
		public cls_Distribution Min;
		public string Desc;
		public cls_Gens_Rem_Dist(string Description) { // constructor
			Max = new cls_Distribution(5);
			Avg = new cls_Distribution(5);
			Min = new cls_Distribution(5);
			Desc = "\n" + Description + " Generations Removed ";
		}
		public void Add_to_Dist(int Gens_Rem_Max, double Gens_Rem_Avg, int Gens_Rem_Min) {
			Max.Add_to_Dist(Gens_Rem_Max);
			Avg.Add_to_Dist((int)(0.5 + Gens_Rem_Avg)); // could do distribution resolutions other than 1 per generation here (0.25 + 2 * ...Avg, etc)
			Min.Add_to_Dist(Gens_Rem_Min);
		}
	}
	public struct struct_Result_Sample {
		// one of these for each sample of each permutation. This will be a 1-D array,
		// but will be in a structure that is a 2-D array so there will be 3-D of this.
		public double Years_to_Max_Pop;
		public double Terminal_Growth_Rate;
		public int Peak_Population;
		public double[] Pop_at_Date;
		public double[] Date_at_Pop;
		public void Init() {
			Years_to_Max_Pop = 0;
			Terminal_Growth_Rate = 0;
			Peak_Population = 0;
			Pop_at_Date = new double[Globals.Report_Pop_at_Date.Length];
			Date_at_Pop = new double[Globals.Report_Date_at_Pop.Length];
		}
	}
	public struct struct_Results {
		// one of these for each permutation. (2 D array: Fema_Min_CBA_Plan , Min_Gap_Between_Ch_Plan )
		public struct_Result_Sample[] Sample;
		public struct_Result_Sample SamplAvg;
		public double Tot_Avg_Term_Growth_Rate;
		public Int64[] Male_Distrb; // number of males alive at the end of a run (index is age)
		public Int64[] Male_Deaths; // number of males who died (index is age at death)
		public Int64[] Fema_Distrb; // number of females alive at the end of a run (index is age)
		public Int64[] Fema_Deaths; // number of females who died (index is age at death)
		public Int64[] Children_Per_Father; // index is number of children. value is number of fathers with that many children.
		public Int64[] Children_Per_Mother; // index is number of children. value is number of mothers with that many children.
		public Int64[] Peak_Wives_Per_Man;  // index is peak number of wives at a time. value is number of men with that many wives.
		public Int64[] Marriages_Per_Man;   // index is number of wives overall. value is number of males with that many wives.
		public Int64[] Marriages_Per_Wife;  // index is number of husbands. value is number of females with that many husbands.
		public Int64[] Marriages_Per_AgeDif;  // index is 100 * (1 + (m.age - f.age) / (m.age + f.age)
		public Int64 Dead_Boys;
		public Int64 Dead_Girls;
		public Int64 Dead_Available_Ladies;
		public Int64 Dead_Widows_PCBA;
		public Int64 Dead_Men;
		public Int64 Dead_Wives_by_Age;
		public Int64 Dead_Wives_by_ChildBirth;
		public int Extinction_Count; // number of samples for this permutation which resulted in extinction.
		public int Non_Extinct_Samples;
		// these counts are the category counts at the end of a run.
		public Int64 Count_Boys;
		public Int64 Count_Girls;
		public Int64 Count_Available_Ladies;
		public Int64 Count_Widows_PCBA;
		public Int64 Count_Men;
		public Int64 Count_Wives;
		public static int Max_Child_Per_Father = 0;
		public static int Max_Child_Per_Mother = 0;
		public static int Max_Marriages_Per_Man = 0;
		public static int Max_Marriages_Per_Woman = 0;
		public static int Min_Marriage_AgeDif = 200;
		public static int Max_Marriage_AgeDif = 0;
		private static int i;
		public int n;
		public int[] Pop_At_Iter_Count;
		public cls_Gens_Rem_Dist[] Gens_Rem_Dist; // index is which pop/date point distribution is taken.
		public void Init() {
			SamplAvg.Init();
			Gens_Rem_Dist = new cls_Gens_Rem_Dist[1]; //~~ maybe add more points here.
			Gens_Rem_Dist[0] = new cls_Gens_Rem_Dist("End of Iteration");
			Sample = new struct_Result_Sample[Globals.Samples_Per_Permutation];
			for (i = 0; i < Globals.Samples_Per_Permutation; i++) Sample[i].Init();
			Male_Distrb = new Int64[Globals.Max_Either_Age];
			Male_Deaths = new Int64[Globals.Max_Either_Age];
			Fema_Distrb = new Int64[Globals.Max_Either_Age];
			Fema_Deaths = new Int64[Globals.Max_Either_Age];
			Marriages_Per_Man = new Int64[Globals.Max_Wives + 2];
			Marriages_Per_Wife = new Int64[4];
			Children_Per_Mother = new Int64[3];
			Children_Per_Father = new Int64[6];
			Marriages_Per_AgeDif = new Int64[201];
			Peak_Wives_Per_Man = new Int64[1 + Globals.Max_Wives];
			Pop_At_Iter_Count = new int[500];
		}
		public void Accumulate_Marriage_By_AgeDif(int iAgeDif) {
			Marriages_Per_AgeDif[iAgeDif]++;
			if (Max_Marriage_AgeDif < iAgeDif) Max_Marriage_AgeDif = iAgeDif;
			if (Min_Marriage_AgeDif > iAgeDif) Min_Marriage_AgeDif = iAgeDif;
		}
		public void Kill_Male(int iAge, int Num_Children, int Peak_Wives, int Num_Marriages) {
			// call this when a male dies.

			// accumulate the distribution of age of man at death.
			Male_Deaths[iAge]++;

			Peak_Wives_Per_Man[Peak_Wives]++;

			// accumulate the distribution of children per man.
			if (Num_Children >= Children_Per_Father.Length) {
				Array.Resize<Int64>(ref Children_Per_Father, Num_Children + 1);
			}
			if (Max_Child_Per_Father < Num_Children) Max_Child_Per_Father = Num_Children;
			Children_Per_Father[Num_Children]++;
		
			// accumulate the distribution of marriages per man.
			if (Num_Marriages >= Marriages_Per_Man.Length) {
				Array.Resize<Int64>(ref Marriages_Per_Man, Num_Marriages + 1);
			}
			Marriages_Per_Man[Num_Marriages]++;
			if (Num_Marriages > Max_Marriages_Per_Man) Max_Marriages_Per_Man = Num_Marriages;
		}
		public void Kill_Fema(int iAge, int Num_Children, int Num_Marriages) {
			// call this when a female dies.

			// accumulate the distribution of age of woman at death.
			Fema_Deaths[iAge]++;

			// accumulate the distribution of children per woman.
			if (Num_Children >= Children_Per_Mother.Length) {
				Array.Resize<Int64>(ref Children_Per_Mother, Num_Children + 1);
			}
			if (Max_Child_Per_Mother < Num_Children) Max_Child_Per_Mother = Num_Children;
			Children_Per_Mother[Num_Children]++;

			// accumulate the distribution of marriages per woman.
			if (Num_Marriages >= Marriages_Per_Wife.Length) {
				Array.Resize<Int64>(ref Marriages_Per_Wife, Num_Marriages + 1);
			}
			Marriages_Per_Wife[Num_Marriages]++;
			if (Num_Marriages > Max_Marriages_Per_Woman) Max_Marriages_Per_Woman = Num_Marriages;
		}
		public void Get_Age_Dist() {
			// captures the age distribution for all living people
			Globals.Girls.Update_Age_Dist(ref Fema_Distrb);
			Globals.Boys.Update_Age_Dist(ref Male_Distrb);
			Globals.Available_Ladies.Update_Age_Dist(ref Fema_Distrb);
			Globals.Widows_PCBA.Update_Age_Dist(ref Fema_Distrb);
			Globals.Households.Update_Age_Dist(ref Male_Distrb, ref Fema_Distrb);
		}
		public void Capture_Target_Data(){
			Sample[Globals.Permutation_Sample_Num].Years_to_Max_Pop = Globals.Iter_Date;
			Sample[Globals.Permutation_Sample_Num].Peak_Population = Globals.Peak_Pop;
			if (Globals.Total_Pop > 0) {
				Non_Extinct_Samples++;
				Count_Boys += Globals.Boys.Count;
				Count_Girls += Globals.Girls.Count;
				Count_Available_Ladies += Globals.Available_Ladies.Count;
				Count_Widows_PCBA += Globals.Widows_PCBA.Count;
				Count_Men += Globals.Households.Count;
				Count_Wives += Globals.Wives_Count;
				Sample[Globals.Permutation_Sample_Num].Terminal_Growth_Rate = Globals.Term_Growth_Rate;
			} else {
				Extinction_Count++;
				Sample[Globals.Permutation_Sample_Num].Terminal_Growth_Rate = 0;
			}	
		}
		public void Append_StringBuilders() {
			Globals.Result_YTT.Append("," + SamplAvg.Years_to_Max_Pop.ToString());
			Globals.Result_TATGR.Append("," + Tot_Avg_Term_Growth_Rate.ToString());
			Globals.Result_NETGR.Append("," + SamplAvg.Terminal_Growth_Rate.ToString());
			Globals.Result_PkP.Append("," + SamplAvg.Peak_Population.ToString());
			Globals.Result_Ext.Append("," + Extinction_Count.ToString());
			Globals.Dead_Boys.Append("," + Dead_Boys.ToString());
			Globals.Dead_Girls.Append("," + Dead_Girls.ToString());
			Globals.Dead_Available_Ladies.Append("," + Dead_Available_Ladies.ToString());
			Globals.Dead_Widows_PCBA.Append("," + Dead_Widows_PCBA.ToString());
			Globals.Dead_Men.Append("," + Dead_Men.ToString());
			Globals.Dead_Wives_by_Age.Append("," + Dead_Wives_by_Age.ToString());
			Globals.Dead_Wives_by_ChildBirth.Append("," + Dead_Wives_by_ChildBirth.ToString());
		}
		public void Pop_At_Iter_Count_Clear() {
			for (int x = 0; x < Pop_At_Iter_Count.Length; x++) Pop_At_Iter_Count[x] = 0;
		}
		public void Pop_At_Iter_Count_Set() {
			if (Globals.Iter_Count >= Pop_At_Iter_Count.Length) {
				Array.Resize<int>(ref Pop_At_Iter_Count, Globals.Iter_Count + 100);
			}
			Pop_At_Iter_Count[Globals.Iter_Count] = Globals.Total_Pop;
		}
	}
	public struct struct_Household {
		public struct_Person Man;
		public struct_Person[] Wives;
		public int Wife_Count;
		public int Peak_Wives; // the most wives the man ever had at one time.
		public static int w;
		private static double dAge;
		private static int iAge;
		private static bool Man_PCBA;
		public void Construct() {
			// initialize array and counts - only executed at the beginning of the program - not when a boy is promoted to a man.
			Wives = new struct_Person[Globals.Max_Wives];
		}
		public void New(ref struct_Person From_Boy) {
			// promote boy to man in previously existing array element. reset everything.
			Wife_Count = 0;
			Peak_Wives = 0;
			Man.Transfer_From(ref From_Boy);
		}
		public void New(int Num_Wives) {
			// overload to initialize a founding marriage at the beginning of a run.
			// add husbands and wife/wives to be the minimum child bearing age.
			Man.BirthDate = -Globals.Male.Min_CBA;
			Man.Num_Children = 0;
			Man.Num_Marriages = Num_Wives;
			Man.Gens_Removed_Min = 0;
			Man.Gens_Removed_Avg = 0;
			Man.Gens_Removed_Max = 0;
			Peak_Wives = Num_Wives;
			Wife_Count = 0;
			while (Wife_Count < Num_Wives) {
				Wives[Wife_Count].Min_Next_Child = Globals.Minimum_Gestation;
				Wives[Wife_Count].Num_Children = 0;
				Wives[Wife_Count].Gens_Removed_Max = 0;
				Wives[Wife_Count].Gens_Removed_Avg = 0;
				Wives[Wife_Count].Gens_Removed_Min = 0;
				Wives[Wife_Count++].BirthDate = -Globals.Fema.Min_CBA;
			}
			Globals.Wives_Count += Num_Wives;
		}
		public bool Scan() {
			// scan one man and his wives (if any). return true if man died.
			dAge = Globals.Iter_Date - Man.BirthDate;
			iAge = (int)dAge;

			// first see if the man can get married or has died.
			if (Globals.Male_Died(iAge)) {
				// the man died. transfer his widows to the available ladies or widows PCBA list depending on their ages
				Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Kill_Male(iAge, Man.Num_Children, Peak_Wives, Man.Num_Marriages);
				w = Wife_Count;
				Globals.Wives_Count -= Wife_Count;
				while (w-- > 0) {
					if ((Globals.Iter_Date - Wives[w].BirthDate) < Globals.Fema.Max_CBA) {
						// she's young enough to get married again and have more kids.
						Globals.Available_Ladies.Add(ref Wives[w]);
					} else { // past childbearing age - she is a widow who will not marry again.
						Globals.Widows_PCBA.Add(ref Wives[w]);
					}
				}
				return true; // man died
			}
			if (Globals.Guy_Gets_Girl(dAge, Wife_Count)) {
				// there was a lady available, he's not maxxed out on wives, and he was lucky enough to marry her.
				// transfer available lady's info to here & remove her from available list.
				Man.Num_Marriages++;
				Globals.Available_Ladies.Get_Wife(ref Wives[Wife_Count++]);
				// remember the most wives this guy ever had at the same time.
				if (Peak_Wives < Wife_Count) Peak_Wives = Wife_Count;
			}

			// now run through the wives and see if they either died or gave birth.
			Man_PCBA = dAge > Globals.Male.Max_CBA;
			w = Wife_Count;
			while (w-- > 0) {
				if (Wives[w].Scan_Wife(ref Man, Man_PCBA)) {
					// she died.
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nMan " + List_of_Households.i.ToString() + "'s wife #" + w.ToString() + " died.");
					iAge = (int)(Globals.Iter_Date - Wives[w].BirthDate);
					Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Kill_Fema(iAge, Wives[w].Num_Children, Wives[w].Num_Marriages);
					Globals.Wives_Count--;
					if (--Wife_Count > w) {
						// there is at least one live wife "above" this one - move the last one down to this spot.
						if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append(" overwriting wife from #" + Wife_Count.ToString());
						Wives[w].Transfer_From(ref Wives[Wife_Count]);
					}
				}
			}
			// man heading household did not die.
			return false;
		}
		public void Transfer_From(ref struct_Household Other_Household) {
			Man.Transfer_From(ref Other_Household.Man);
			Wife_Count = Other_Household.Wife_Count;
			Peak_Wives = Other_Household.Peak_Wives;
			for (w = Wife_Count - 1; w >= 0; w--) {
				Wives[w].Transfer_From(ref Other_Household.Wives[w]);
			}
		}
		public void Update_Age_Dist(ref Int64[] Male_Distrb, ref Int64[] Fema_Distrb) {
			Male_Distrb[(int)(Globals.Iter_Date - Man.BirthDate)]++;
			w = Wife_Count;
			while (w-- > 0) Fema_Distrb[(int)(Globals.Iter_Date - Wives[w].BirthDate)]++;
		}
		public void Debug_Trace_Append_Man() {
			Globals.Debug_Trace.Append("," + Man.BirthDate.ToString(Globals.DateFormat));
			Globals.Debug_NumChildren.Append("," + Man.Num_Children.ToString());
			Globals.Debug_NumMarriages.Append("," + Man.Num_Marriages.ToString());
		}
		public void Debug_Trace_Append_Wife() {
			if (Wife_Count > w) {
				Wives[w].Debug_Trace_Append();
			} else {
				// man does not have this wife #. put blanks underneath.
				Globals.Debug_Trace.Append(",");
				Globals.Debug_NumChildren.Append(",");
				Globals.Debug_MinNextChild.Append(",");
				Globals.Debug_NumMarriages.Append(",");
			}
		}
		public void Capture_Gens_Rem_Dist() {
			// run through all living individuals in this household and update the generations-removed distribution.
			Man.Capture_Gens_Rem_Dist();
			w = Wife_Count;
			while (w-- > 0) Wives[w].Capture_Gens_Rem_Dist();
		}
	}
	public struct struct_Per_Date_Tots {
		public double Iter_Date;
		public int Tot_Pop;
	}
	#endregion
	public class List_of_Individuals {
		#region "Definitions"
		/*		This Class handles lists of individuals. Because there are separate lists for the different categories
		 *		(boys, girls, available women, and widows/spinsters PCBA (past child bearing age), we do not need to
		 *		store any information other than the birthdate - a double-precision floating point representing the
		 *		number of years after the start date. A negative birth date indicates the individual was born before
		 *		the start date (example: Shem was born 98 years before the flood, so for a simulation of post-Flood
		 *		population growth, his birth date would be -98.
		 *		Note that men (any male above child-bearing age, which includes available, married and widowered) are
		 *		handled by a separate class that also handles their wife or wives. For married women we must also track
		 *		the minimum time before bearing another child, so they are handled by that class as well.  	*/
		public struct_Person[] Individuals; // array of people.
		private static int i;
		private static double dAge;
		private static int iAge;
		public bool Is_Male = false;
		public int Array_Resizes = 0;
		public int Count = 0;
		private int Peak_Count = 0;
		#endregion
		#region "Add / Init Routines"
		public List_of_Individuals(int Initial_Size) {
			// constructor - called when a new list is created.
			// initialize the array. Initial size must be at least 1.
			if (Initial_Size < 1) Initial_Size = 1;
			Individuals = new struct_Person[Initial_Size];
			Is_Male = false; // by default. Can be changed after construction by creating routine.
		}
		public void Add(double BirthDate, int GenRem_Max, double GenRem_Avg, int GenRem_Min) {
			// overload to add a new person to this list.
			if (Globals.Track_Babies) {
				// only do this up to the target. After that, count children born, but do not track them.
				Individuals[Count++].New(BirthDate, GenRem_Max, GenRem_Avg, GenRem_Min);
				Add_Elements_As_Needed();
			}
		}
		public void Add(ref struct_Person Individual) {
			// add a previously existing person to this list.
			Individuals[Count++].Transfer_From(ref Individual);
			Add_Elements_As_Needed();
		}
		private void Add_Elements_As_Needed() {
			// make sure there is a spot for the next person.
			if (Peak_Count < Count) Peak_Count = Count; // for diagnostic purposes.
			if (Count >= Individuals.Length) {
				/*		the array is not big enough to hold the next individual, so increase its size. This should
				 *		only happen a few times in an entire run & therefore will not consume much processor time.
				 *		Also note the array is private so there is no danger of other references to this array not
				 *		getting updated due to this re-sizing.		*/
				Array.Resize<struct_Person>(ref Individuals, 5 * Count / 4);
				Array_Resizes++;
			}
		}
		public string Capacity() {
			return Individuals.Length.ToString() + ",";
		}
		public string strPeakCt() {
			return Peak_Count.ToString() + ",";
		}
		public void Clear() {
			Count = 0;
		}
		public void Get_Wife(ref struct_Person Bride) {
			// This is only used for the available ladies list.
			// remove the randomly picked lady from this list and set the calling routine's wife's birthdate & number of children.
			i = Globals.xAvailLady;
			Bride.Transfer_From(ref Individuals[i]);
			Remove_from_List();
			// first child born at minimum of 0.8 years after she gets married. or more if she recently had one.
			Bride.Min_Next_Child = Globals.Iter_Date + Globals.Minimum_Gestation;
			if (Individuals[Count].Min_Next_Child > Bride.Min_Next_Child) Bride.Min_Next_Child = Individuals[Count].Min_Next_Child;
			Bride.Num_Marriages++;
		}
		private void Remove_from_List() {
			// removes Individuals[i] from the list by moving the last active element on the list on top of [i] and decrement the count.
			if (i >= --Count) return; // if this was the last individual on the list, don't bother overwriting.
			Individuals[i].Transfer_From(ref Individuals[Count]);
		}
		public void Update_Age_Dist(ref Int64[] Distrb) {
			i = Count;
			while (i-- > 0) {
				Distrb[(int)(Globals.Iter_Date - Individuals[i].BirthDate)]++;
			}
		}
		public void Capture_Gens_Rem_Dist() {
			// run through all living individuals and update the generations-removed distribution.
			i = Count;
			while (i-- > 0) Individuals[i].Capture_Gens_Rem_Dist();
		}
		#endregion
		#region "Scan Routines"
		public void Scan_Boys() {
			/*		This list of individuals is a list of boys.
			 *		Run through them and randomly kill a few off, and promote them to be heads of households when old enough.
			 *		Note that this and the next three routines have a lot of common code. They are separate so that it will run
			 *		faster by not having to ask "is this a boy / girl / widow / available lady" on every pass of the loop.   */
			i = Count;
			while (i-- > 0) {
				dAge = Globals.Iter_Date - Individuals[i].BirthDate;
				iAge = (int)dAge;
				if (dAge >= Globals.Male.Min_CBA) {
					// boy made it to the age at which he becomes a head of household. add him to that list and remove from this one.
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nBoy " + i.ToString() + " is of age. Promoting to man " + Globals.Households.Count.ToString() + " and overwriting boy from " + (Count - 1).ToString());
					Globals.Households.Add(ref Individuals[i]);
					Remove_from_List();
				} else if (Globals.Male_Died(iAge)) {
					// oooh, bad luck. Died in childhood. remove from the list.
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nBoy " + i.ToString() + " died. Overwriting Boy from " + (Count - 1).ToString());
					Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Dead_Boys++;
					Individual_Died();
				}
			}
		}
		public void Scan_Girls() {
			/*		This list of individuals is a list of girls.
			 *		Run through them and randomly kill a few off, and promote them to be available for marriage when old enough.	*/
			i = Count;
			while (i-- > 0) {
				dAge = Globals.Iter_Date - Individuals[i].BirthDate;
				iAge = (int)dAge;
				if (dAge >= Globals.Fema.Min_CBA) {
					// girl made it to the available age. add her to the list of available ladies and remove from this one.
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nGirl " + i.ToString() + " is of age. Promoting to available lady " + Globals.Available_Ladies.Count.ToString() + " and overwriting girl from " + (Count - 1).ToString());
					Globals.Available_Ladies.Add(ref Individuals[i]);
					Remove_from_List();
				} else if (Globals.Fema_Died(iAge)) {
					// oooh, bad luck. Died in childhood. remove from the list.
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nGirl " + i.ToString() + " died. Overwriting Girl from " + (Count - 1).ToString());
					Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Dead_Girls++;
					Individual_Died();
				}
			}
		}
		public void Scan_Widows_PCBA() {
			/*		This list of individuals is a list of single women past childbearing age (widows and spinsters).
			 *		Run through them and randomly kill them off.		*/
			i = Count;
			while (i-- > 0) {
				dAge = Globals.Iter_Date - Individuals[i].BirthDate;
				iAge = (int)dAge;
				if (Globals.Fema_Died(iAge)) {
					// Died of old age. remove from the list.
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nWidow " + i.ToString() + " died. Overwriting Widow from " + (Count - 1).ToString());
					Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Dead_Widows_PCBA++;
					Individual_Died();
				}
			}
		}
		public void Scan_Available_Ladies() {
			/*		This list of individuals is a list of available women.
			 *		Run through them and randomly kill a few off, and promote them to the widow/spinster list when too old.
			 *		Note that the most common way of getting removed from this list is not handled here. Most ladies will leave
			 *		this list almost immediately after being added to it when a man marries her and adds her to his household.  */
			i = Count;
			while (i-- > 0) {
				dAge = Globals.Iter_Date - Individuals[i].BirthDate;
				iAge = (int)dAge;
				if (dAge >= Globals.Fema.Max_CBA) {
					// She is now past child-bearing age. Add her to that list and remove from this one.
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nAvailable Lady " + i.ToString() + " is PCBA. Moving to Widows_PCBA " + Globals.Widows_PCBA.Count.ToString() + " and overwriting AL from " + (Count - 1).ToString());
					Globals.Widows_PCBA.Add(ref Individuals[i]);
					Remove_from_List();
				} else if (Globals.Fema_Died(iAge)) {
					// oooh, bad luck. Died while available. Remove from the list.
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nAvailable Lady " + i.ToString() + " died. Overwriting AL from " + (Count - 1).ToString());
					Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Dead_Available_Ladies++;
					Individual_Died();
				}
			}
		}
		private void Individual_Died() {
			if (Is_Male) { // killed a boy.
				Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Kill_Male(iAge, 0, 0, 0);
			} else { // killed a girl or single woman
				Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Kill_Fema(iAge, Individuals[i].Num_Children, Individuals[i].Num_Marriages);
			}
			Remove_from_List();
		}
		public void Debug_Trace_Summary(string desc) {
			Globals.Debug_Trace.Append("\n" + desc + " count = " + Count.ToString());
			if (Count < 1) return;
			Globals.Debug_MinNextChild.Length = 0;
			Globals.Debug_NumChildren.Length = 0;
			Globals.Debug_NumMarriages.Length = 0;
			Globals.Debug_Trace.Append("\nBirthDates");
			Globals.Debug_NumChildren.Append("\nNum Children");
			Globals.Debug_MinNextChild.Append("\nMin Next Child");
			Globals.Debug_NumMarriages.Append("\nNum Marriages");
			for (i = 0; i < Count; i++) {
				Individuals[i].Debug_Trace_Append();
			}
			Globals.Debug_Trace.Append(Globals.Debug_NumChildren.ToString() + Globals.Debug_NumMarriages.ToString() + Globals.Debug_MinNextChild.ToString());
		}
		#endregion
	}
	public class List_of_Households {
		#region "Definitions"
		/*		Class handles a list of men and their wives, if any. There will only be one of these lists.
		 *		Households have one husband and a list of wives. If the man dies, the marriage ends and the widows
		 *		(if any) are returned to the available ladies list if young, or to the Widows_PCBA list for widows
		 *		past child-bearing age.	If a wife dies, she is just removed from the list.
		 *		For the man we only need the birth date & # of children. For each wife we need her birthdate, # of
		 *		children, and the minimum date on which her next child might be born.
		 *
		 *		Polygamy is handled as follows:
		 *		- Polyandry (multiple husbands in a marriage) is not handled by this program.
		 *		- The wives' array for each element of the mens' array is initialized with the max number of wives.
		 *			This wastes memory but optimizes speed by never requiring any resizing or locating an
		 *			appropriately sized element.
		 *		- The probability per scan of getting married should be moderately high for bachelors.
		 *		- For monogamy-only simulations, the probability of taking a 2nd wife should be 0.
		 *		- For polygynous (multiple wives per marriage) simulations, the probability of taking a 2nd wife
		 *			should be very low but >0.
		 *		- For polygynous simulations, the probability of taking a 3rd or higher wife should be higher than
		 *			taking a 2nd wife, but lower than taking a first wife.	*/
		private struct_Household[] Households; // array of men and their potential wives.
		public static int i;
		public int Count = 0;
		private int Peak_Count = 0;
		public int Array_Resizes = 0;
		#endregion
		#region "Routines"
		public List_of_Households(int Init_Size) {
			// constructor - called when a new list is created.
			// note that unlike the individuals lists, this is not designed to grow.
			Households = new struct_Household[Init_Size];
			for (i = 0; i < Init_Size; i++) Households[i].Construct();
		}
		public void Add(ref struct_Person Boy) {
			// promote a boy to this list.
			Households[Count++].New(ref Boy);
			Add_Elements_As_Needed();
		}
		public void Add(int Num_Wives) {
			// overload to initialize the founding marriage(s) at the beginning of a run.
			// add husbands and wife/wives to be the minimum child bearing age.
			Households[Count++].New(Num_Wives);
		}
		private void Add_Elements_As_Needed() {
			// make sure there is a spot for the next household.
			if (Peak_Count < Count) Peak_Count = Count; // for diagnostic purposes. 
			if (Count >= Households.Length) {
				/*		the array is not big enough to hold the next household, so increase its size. This should
				 *		only happen a few times in an entire run & therefore will not consume much processor time.
				 *		Also note the array is private so there is no danger of other references to this array not
				 *		getting updated due to this re-sizing.		*/
				Array.Resize<struct_Household>(ref Households, 5 * Count / 4);
				Array_Resizes++;
				for (i = Count; i < Households.Length; i++) Households[i].Construct();
			}
		}
		public void Clear() {
			Count = 0;
			Globals.Wives_Count = 0;
		}
		public void Scan_Households() {
			// Run through this list of men and their wives.
			i = Count;
			while (i-- > 0) {
				if (Households[i].Scan()) {
					// this man died. the Scan routine moved his widows to their lists. remove him from this list.
					Globals.Results[Globals.xCBA_Plan, Globals.xMin_Gap_Between_Ch_Plan].Dead_Men++;
					Count--;
					if (Globals.Output_Debug_Trace) Globals.Debug_Trace.Append("\nMan " + i.ToString() + " died. Moving Man " + Count.ToString() + " down.");
					// unless the guy that died was at the end of the list, move the last one down over the dead one.
					if (i < Count) Households[i].Transfer_From(ref Households[Count]);
				}
			}
		}
		public void Update_Age_Dist(ref Int64[] Male_Distrb, ref Int64[] Fema_Distrb) {
			i = Count;
			while (i-- > 0) Households[i].Update_Age_Dist(ref Male_Distrb, ref Fema_Distrb);
		}
		public string Capacity() {
			return Households.Length.ToString() + ",";
		}
		public string strCount() {
			return Count.ToString();
		}
		public string strPeakCt() {
			return Peak_Count.ToString() + ",";
		}
		public void Debug_Trace_Summary() {
			Globals.Debug_Trace.Append("\nHousehoulds (men) count = " + Count.ToString());
			if (Count < 1) return;
			string wf;
			Globals.Debug_NumChildren.Length = 0;
			Globals.Debug_NumMarriages.Length = 0;
			Globals.Debug_Trace.Append("\nBirthDates");
			Globals.Debug_NumChildren.Append("\nNum Children");
			Globals.Debug_NumMarriages.Append("\nNum Marriages");
			for (i = 0; i < Count; i++) {
				Households[i].Debug_Trace_Append_Man();
			}
			Globals.Debug_Trace.Append(Globals.Debug_NumChildren.ToString());
			Globals.Debug_Trace.Append(Globals.Debug_NumMarriages.ToString());
			for (struct_Household.w = 0; struct_Household.w < Globals.Max_Wives; struct_Household.w++) {
				Globals.Debug_NumChildren.Length = 0;
				Globals.Debug_NumMarriages.Length = 0;
				Globals.Debug_MinNextChild.Length = 0;
				wf = "\nWife #" + struct_Household.w.ToString();
				Globals.Debug_Trace.Append(wf + " BirthDates");
				Globals.Debug_NumChildren.Append(wf + "Num Children");
				Globals.Debug_MinNextChild.Append(wf + "Min Next Child");
				Globals.Debug_NumMarriages.Append(wf + "Num Marriages");
				for (i = 0; i < Count; i++) {
					Households[i].Debug_Trace_Append_Wife();
				}
				Globals.Debug_Trace.Append(Globals.Debug_NumChildren.ToString() + Globals.Debug_NumMarriages.ToString() + Globals.Debug_MinNextChild.ToString());
			}
		}
		public void Capture_Gens_Rem_Dist() {
			// run through all living individuals in all households and update the generations-removed distribution.
			i = Count;
			while (i-- > 0) Households[i].Capture_Gens_Rem_Dist();
		}
		#endregion
	}
	public static class Globals {
		#region "Parameters"
		/*		There are three major areas of this application:
		 *			- The main form.
		 *			- The Individuals class. (children, available women and single women past CBA
		 *			- The Households class (men, single or with any wives.
		 *		This Globals class contains constants and variables that need to be seen inside more than one of those.	*/
		public static double Iterate_Interval = 0.1; // iterate this many times per year. 
		public static string Results_Prefix = "H:\\Docs\\Chris\\My_Code\\PopGen2\\results\\";
		public static int Samples_Per_Permutation = 10;
		private static double MS_Between_Iteration_Breaks = 100; // let windows get in & do stuff this often.
		public static int Initial_Couples = 12;
		/* the two biggest factors influencing growth rate are the age at which the mother starts bearing children and
		 * the spacing between children. This program is designed to iterate many samples on all permutations of likely
		 * values for those two factors. */
		public static double[] Fema_Min_CBA_Plan = { 14 };
		public static double[] Fema_Max_CBA_Plan = { 44 };
		public static double[] Male_Min_CBA_Plan = { 14 };
		public static double[] Male_Max_CBA_Plan = { 9999 };
		public static double[] Min_Gap_Between_Ch_Plan = { 1 };
		// Pop_at_Date and Date_at_Pop array elements must be >0 and in ascending order to work properly.
		public static double[] Report_Pop_at_Date = { 100 };
		public static double[] Report_Date_at_Pop = { 1000 };
		public static double Minimum_Gestation = 0.8; // years after marriage before child can be born.
		public static double Initial_Annual_Pregnancy_Probability = 0.88;
		public static double CB_Years_at_Init_Pregnancy_Probability = 10;
		private static double Death_in_Childbirth_Probability = 0.001;
		private static double Death_Bearing_Twins_Probability = 0.01;
		private static double Twin_Probability = 1.0 / 90.0;
		private static double Probability_Baby_is_Male = 0.51;
		public static double Stop_Iterating_After_Year = 430;
		public static int Stop_Iterating_After_Pop = 10000;
		private static double First_Marriage_Probability_Per_Year = 0.5;
		private static double Second_Marriage_Probability_Per_Year = 0.01;
		private static double Third_Plus_Marriage_Probability_Per_Year = 0.05;
		public static int Max_Wives = 5;  // the most wives any guy can have at one time. (1=purely monogamous simulation)
		public static double Man_Too_Old_to_Marry = 70; // he can keep having kids, but no more new wives after this age.
		// note that parameters which depend on sex are set in the constructor routine below - see Male.Init() and Fema.Init().
		#endregion
		#region "Variables"
		public static string Parameter_File_Name = "Program Defaults";
		public static string Results_Path;
		public static string DateFormat;
		public static bool Track_Babies; // true while approaching target population / years. False afterward.
		private static double[] Marriage_Probability_Per_Iter; // probability of marriage each iteration based on number of wives.
		private static double[] Pregnancy_Probability_perIter; // index is age of woman.
		public static double Iter_Date; // date of the current iteration.
		public static int Iter_Count; // count of the current iteration (is Date / Iterate_Interval)
		public static struct_Info_by_Sex Male;
		public static struct_Info_by_Sex Fema;
		public static int Max_Either_Age;
		public static int Wives_Count; // total wives living in all households.
		public static List_of_Individuals Boys;
		public static List_of_Individuals Girls;
		public static List_of_Individuals Available_Ladies;
		public static List_of_Individuals Widows_PCBA; // widows past child-bearing age.
		public static List_of_Households Households; // men - either single or with their wives.
		public static bool Childbirth_Mother_Died = false; // result of Have_a_Baby
		public static bool Childbirth_Girl = false; //			result of Have_a_Baby
		public static bool Childbirth_Boy = false; //			result of Have_a_Baby
		public static double Min_Gap_Between_Children; // set from the permutation plan array above.
		public static int Total_Pop = 0;
		// Peak_Pop will be the same as Total_Pop for a growing population, but is interesting for populations that go extinct.
		public static int Peak_Pop = 0;
		private static Random RandGen; // pseudo-random number generator.
		public static int xCBA_Plan = 0; // index for iterating through the elements of the permutation plan array
		public static int xMin_Gap_Between_Ch_Plan = 0; // index for iterating through the elements of the permutation plan array
		private static int xPop_at_Date;
		private static int xDate_at_Pop;
		/*		Calculate the terminal growth rate based on the years required for the population to increase the last
		 *		order of magnitude (to go from Stop_Iterating_After_Pop/10 to Stop_Iterating_After_Pop). Once the total
		 *		population is past 10,000, the stochastic jumpiness of small populations is no longer significant and
		 *		traditional population growth math is more accurate.  */
		private static struct_Per_Date_Tots[] Per_Date_Tots;
		private static int xPer_Date_Tots;
		public static int Permutation_Sample_Num = 0;
		/* This program executes the calculations on a timer tick that runs (at its fastest) every 1 millisecond.
		 * This allows the program to take brief breaks to allow other windows processes to execute without bogging down,
		 * including updating the form graphics so that the progress of the simulation can be monitored.
		 * If we are running 100 samples per permutation, with 10 CBA-start and 14 child gap settings, that's 14,000 runs,
		 * each of which could include 10,000 iterations if we go 1000 years 10 times per year. Obviously we need to do a
		 * lot more than one iteration each tick. */
		public static StringBuilder Debug_Trace; // for diagnostic purposes, output the various population counts at each iteration.
		public static StringBuilder Result_YTT; // Grand_Averages text: parameter list and years-to-target population grid
		public static StringBuilder Result_NETGR; // Grand_Averages text: average non-extinct terminal growth rate grid
		public static StringBuilder Result_TATGR; // Grand_Averages text: total average terminal growth rate grid
		public static StringBuilder Result_Ext; // Grand_Averages text: extinction count grid.
		public static StringBuilder Result_PkP; // Grand_Averages text: peak population
		public static StringBuilder Result_SampDet; // sample_summary text
		public static StringBuilder Debug_NumChildren;
		public static StringBuilder Debug_NumMarriages;
		public static StringBuilder Debug_MinNextChild;
		private static StringBuilder PermHeader1;
		private static StringBuilder PermHeader2;
		private static StringBuilder PermHeader3;
		private static StringBuilder PermHeader4;
		private static StringBuilder PermHeader5;
		private static string Permutation_header = "";
		private static StringBuilder SummaryHeader1;
		private static StringBuilder SummaryHeader2;
		private static StringBuilder SummaryHeader3;
		private static StringBuilder SummaryHeader4;
		private static string table_header = "";
		public static StringBuilder Dead_Boys;
		public static StringBuilder Dead_Girls;
		public static StringBuilder Dead_Available_Ladies;
		public static StringBuilder Dead_Widows_PCBA;
		public static StringBuilder Dead_Men;
		public static StringBuilder Dead_Wives_by_Age;
		public static StringBuilder Dead_Wives_by_ChildBirth;
		public static StringBuilder Gen_Rem_Dist_Max;
		public static StringBuilder Gen_Rem_Dist_Avg;
		public static StringBuilder Gen_Rem_Dist_Min;
		public static struct_Results[,] Results;
		public static bool Still_Running = true;
		public static bool Output_Debug_Trace;
		public static bool Iterate_Slowly = false;
		public static int Samples_Run = 0;
		public static double Total_Samples;
		public static DateTime StartTime;
		public static TimeSpan ElapsedTime;
		public static string str_Progress;
		private static DateTime EstEndTime;
		private static double dbl_PctDone = 0;
		public static string str_PctDone = "0.00%";
		private static DateTime Take_a_Break;
		public static bool Already_Executing = false;
		public static int Pause_After_Run = 0;
		public static int Debug_Trace_Blank_Len = 0;
		public static string str_ElapsedTime;
		public static double Term_Growth_Rate;
		private static int i;
		public static string ParamLine;
		private static string str_ParamVal;
		private static double dbl_ParamVal;
		private static bool Set_Male_Max_CBA_to_Max_Age = false;
		private static bool Set_Male_Max_CBA_to_Fema_Max_CBA = false;
		private static bool Set_Male_Min_CBA_to_Fema_Min_CBA = false;
		private static double Offset_Fema_Max_CBA_from_Min_CBA = 0;
		public static int xAvailLady;
		private static double Wife_Age;
		private static double dAgeDif;
		#endregion
		static Globals() { // constructor.
			double d;

			StartTime = DateTime.Now;
			EstEndTime = StartTime.AddSeconds(Total_Samples);
			ElapsedTime = TimeSpan.Zero;
			str_Progress = "";
			DateFormat = "0";
			if (Iterate_Interval < 1) {
				d = Iterate_Interval;
				DateFormat += ".";
				while (d < 0.999) {
					DateFormat += "0";
					d *= 10;
				}
			}

			// if there is a parameter file, these defaults may be immediately overwritten by its contents.
			Fema.Max_Age = 170;
			Fema.Mortality_while_0 = 0.011;
			Fema.Mortality_Point = new List<struct_Actuarial_Point>();
			Male.Max_Age = 150;
			Male.Mortality_while_0 = 0.012;
			Male.Mortality_Point = new List<struct_Actuarial_Point>();
			
			// default parameter file.
			if (Program.First_Arg.Length < 1) Program.First_Arg = @"H:\Docs\Chris\My_Code\PopGen2\results\Flood_Babel_Monog.PopGen";
			// if the user dropped a .PopGen parameter file on this application. parse it.
			if (Program.First_Arg.EndsWith(".PopGen")) Parse_PopGen_Parameters();
			Total_Samples = Samples_Per_Permutation * Min_Gap_Between_Ch_Plan.Length * Fema_Min_CBA_Plan.Length;

			Debug_NumChildren = new StringBuilder();
			Debug_NumMarriages = new StringBuilder();
			Debug_MinNextChild = new StringBuilder();
			Debug_Trace = new StringBuilder();
			Result_YTT = new StringBuilder();
			Result_NETGR = new StringBuilder();
			Result_TATGR = new StringBuilder();
			Result_Ext = new StringBuilder();
			Result_PkP = new StringBuilder();
			Result_SampDet = new StringBuilder();
			PermHeader1 = new StringBuilder("Minimum Female Child Bearing Age,");
			PermHeader2 = new StringBuilder("\nMaximum Female Child Bearing Age,");
			PermHeader3 = new StringBuilder("\nMinimum Male Child Bearing Age,");
			PermHeader4 = new StringBuilder("\nMaximum Male Child Bearing Age,");
			PermHeader5 = new StringBuilder("\nMinimum Gap Between Children,");
			SummaryHeader1 = new StringBuilder("\n");
			SummaryHeader2 = new StringBuilder(",<-- Female Minimum\n");
			SummaryHeader3 = new StringBuilder(",<-- Female Maximum\nMinimum");
			SummaryHeader4 = new StringBuilder(",<-- Male Minimum\nChild Gap");
			Dead_Boys = new StringBuilder();
			Dead_Girls = new StringBuilder();
			Dead_Available_Ladies = new StringBuilder();
			Dead_Widows_PCBA = new StringBuilder();
			Dead_Men = new StringBuilder();
			Dead_Wives_by_Age = new StringBuilder();
			Dead_Wives_by_ChildBirth = new StringBuilder();
			Gen_Rem_Dist_Min = new StringBuilder();
			Gen_Rem_Dist_Avg = new StringBuilder();
			Gen_Rem_Dist_Max = new StringBuilder();
			Per_Date_Tots = new struct_Per_Date_Tots[3 + (int)(Stop_Iterating_After_Year / Iterate_Interval)];

			Results_Path = Results_Prefix + DateTime.Now.ToString("yyyy_MM_dd__HH_mm ") + Parameter_File_Name + "\\";
			// figure out the longer of the maximum age the two sexes and add one in case a fraction is used. This is for array sizing.
			Max_Either_Age = 1 + Higher_of((int)Fema.Max_Age, (int)Male.Max_Age);
			// ****** set constants which depend on sex here *******
			Fema.Init();
			Male.Init();
			RandGen = new Random();
			/*		Marriage probability:
			 *		The probability that a man will take a wife (if one is available) depends on how many wives he already has.
			 *		If he has none, there is a high probability that he will take one within a year. If he has one, there is a
			 *		very low (for polygynous societies) or no (for monogamous societies) chance he will take one. But in
			 *		polygynous societies, once he has two or more wives, the probability of taking another is moderately higher
			 *		than taking the 2nd wife. Finally, the probability of taking a wife when he already has the maximum number
			 *		is zero. We use a negative number to prevent the possibility of crashing on a random number exactly equal 0.	*/
			Marriage_Probability_Per_Iter = new double[1 + Higher_of(Max_Wives, 3)];
			Marriage_Probability_Per_Iter[0] = Probability_PerIter_from_PerYear(First_Marriage_Probability_Per_Year);
			Marriage_Probability_Per_Iter[1] = Probability_PerIter_from_PerYear(Second_Marriage_Probability_Per_Year);
			Marriage_Probability_Per_Iter[2] = Probability_PerIter_from_PerYear(Third_Plus_Marriage_Probability_Per_Year);
			for (i = Max_Wives - 1; i > 2; i--) Marriage_Probability_Per_Iter[i] = Marriage_Probability_Per_Iter[2];
			// there is no chance that a man with the maximum number of wives can get married again.
			Marriage_Probability_Per_Iter[Max_Wives] = -1;

			// pregnancy probability table changes with Min_CBA, so it is calculated in Re_Init.
			Boys = new List_of_Individuals(Stop_Iterating_After_Pop / 2);
			Boys.Is_Male = true;
			Girls = new List_of_Individuals(Stop_Iterating_After_Pop / 2);
			Available_Ladies = new List_of_Individuals(Stop_Iterating_After_Pop / 14);
			Widows_PCBA = new List_of_Individuals(Stop_Iterating_After_Pop / 7);
			Households = new List_of_Households((int)(Stop_Iterating_After_Pop * (0.02 + Probability_Baby_is_Male)));

			Results = new struct_Results[Fema_Min_CBA_Plan.Length, Min_Gap_Between_Ch_Plan.Length];
			for (xMin_Gap_Between_Ch_Plan = 0; xMin_Gap_Between_Ch_Plan < Min_Gap_Between_Ch_Plan.Length; xMin_Gap_Between_Ch_Plan++) {
				for (xCBA_Plan = 0; xCBA_Plan < Fema_Min_CBA_Plan.Length; xCBA_Plan++) {
					Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Init();
				}
			}
			xCBA_Plan = 0;
			xMin_Gap_Between_Ch_Plan = 0;
		}
		#region "Parse Parameter File"
		/* Configuring your computer to use .PopGen parameter files:
		 * 1. Go to the results directory.
		 * 2. Put a sample ".PopGen" file there.
		 *	3. Open the file (perhaps by double-clicking it). Windows will warn you:
		 *			"Windows can't open this file: ... ... What do you want to do?
		 *				Use the web service to find the correct program
		 *				Select a program from a list of installed programs
		 *	4. Choose the lower option and click OK.
		 *	5. Choose Notepad and click OK.
		 *	6. Edit the parameters as needed.
		 *	7. Save and close the .PopGen text file.
		 *	8. Right-click the file and choose "Open With..."
		 *	9. At the bottom of the "Open With" Diaglog box, uncheck:
		 *			"Always use the selected program to open this kind of file"
		 *	10. Click the "Browse..." button.
		 *	11. Browse to the program folder, "bin", "Debug"
		 *	12. Select "PopGen2.exe" from the ...\bin\Debug folder. Click OK.
		 *	
		 * From now on, when you double-click (open) a .PopGen file, it will open in Notepad for editing.
		 * Then when you right-click and choose "Open With", PopGen2 will be there in the menu selection to run. */
		private static void Parse_PopGen_Parameters() {
			string PopGenParams;

			// read the contents of the file into one long string. If that fails, bail out.
			if (!File_Read_Text(Program.First_Arg, out PopGenParams)) return;

			i = Program.First_Arg.LastIndexOf(@"\");
			if (i > 0 && (Program.First_Arg.Length - i) > 8) {
				Parameter_File_Name = Program.First_Arg.Substring(i + 1, Program.First_Arg.Length - i - 8);
			}
			int pgpLen = PopGenParams.Length;
			int p1 = 0;
			int p2;
			// parse one line at a time.
			while (p1 < pgpLen) {
				p2 = PopGenParams.IndexOf("\n", p1);
				if (p2 > p1) {
					ParamLine = PopGenParams.Substring(p1, p2 - p1);
					p1 = p2 + 1;
				} else { // no more new lines. parse this last line and bail.
					ParamLine = PopGenParams.Substring(p1);
					pgpLen = -2;
				}
				Parse_PopGen_Param_Line();
			}
		}
		private static void Parse_PopGen_Param_Line() {
			if (Truncate_Line_After("//")) return; // line started with "//".
			if (Truncate_Line_After(";")) return; // line started with ";".
			if (Truncate_Line_After("'")) return; // line started with "'".
			int XEq = ParamLine.IndexOf("=");
			if (XEq < 0) return; // no equal sign, so no parameter assignment.

			str_ParamVal = ParamLine.Substring(XEq + 1).Trim();
			ParamLine = ParamLine.Substring(0, XEq).Trim().Replace(" ", "");

			if (str_ParamVal.StartsWith(@"""")) {
				// the value is a string. remove the quotes before and after.
				str_ParamVal = str_ParamVal.Substring(1, str_ParamVal.Length - 2);
				// only one string value parameter. handle it now and bail.
				if (ParamLine == "Results_Prefix") {
					Results_Prefix = str_ParamVal;
				} else {
					MessageBox.Show("In the file " + Program.First_Arg + "\nThe parameter " + ParamLine + " was either unknown or should not have had a string value: \n" + str_ParamVal);
				}
				return;
			}
			// it's not a string. get rid of all internal spaces in the value.
			str_ParamVal = str_ParamVal.Replace(" ", "");

			// there are some assignments where variables can be set to other variables. handle these before attempting to evaluate the value string.
			if (ParamLine == "Fema.Max_CBA") {
				const string Min_Age_Offset_Prefix = "Fema.Min_CBA+";
				if (str_ParamVal.StartsWith(Min_Age_Offset_Prefix)) {
					if (double.TryParse(str_ParamVal.Substring(Min_Age_Offset_Prefix.Length), out dbl_ParamVal)) {
						Offset_Fema_Max_CBA_from_Min_CBA = dbl_ParamVal;
					} else {
						MessageBox.Show("In the file " + Program.First_Arg + "\nThe parameter " + ParamLine + " had an invalid offet:\n" + str_ParamVal);
					}
					return;
				}
			}
			if (ParamLine == "Male.Min_CBA") {
				if (str_ParamVal == "Fema.Min_CBA") {
					Set_Male_Min_CBA_to_Fema_Min_CBA = true;
					return;
				}
			}
			if (ParamLine == "Male.Max_CBA") {
				if (str_ParamVal == "Male.Max_Age") {
					Set_Male_Max_CBA_to_Max_Age = true;
					return;
				}
				if (str_ParamVal == "Fema.Max_CBA") {
					Set_Male_Max_CBA_to_Fema_Max_CBA = true;
					return;
				}
			}
			if (str_ParamVal.StartsWith("{")) {
				// the value is an array
				try {
					// convert the string containing a comma-separated list of numbers into a list of doubles.
					// if any member of the list is not a valid double, we will generate an error and fall to the "catch" below.
					string[] strArry = str_ParamVal.Substring(1, str_ParamVal.Length - 2).Split(',');
					switch (ParamLine) { 
						case "Fema.Min_CBA":
							Fema_Min_CBA_Plan = strArry.Select(t => double.Parse(t)).ToArray();
							return;
						case "Fema.Max_CBA":
							Fema_Max_CBA_Plan = strArry.Select(t => double.Parse(t)).ToArray();
							return;
						case "Male.Min_CBA":
							Male_Min_CBA_Plan = strArry.Select(t => double.Parse(t)).ToArray();
							return;
						case "Male.Max_CBA":
							Male_Max_CBA_Plan = strArry.Select(t => double.Parse(t)).ToArray();
							return;
						case "Min_Gap_Between_Children":
							Min_Gap_Between_Ch_Plan = strArry.Select(t => double.Parse(t)).ToArray();
							return;
						case "Report_Pop_at_Date":
							Report_Pop_at_Date = strArry.Select(t => double.Parse(t)).ToArray();
							return;
						case "Report_Date_at_Pop":
							Report_Date_at_Pop = strArry.Select(t => double.Parse(t)).ToArray();
							return;
						default:
							MessageBox.Show("In the file " + Program.First_Arg + "\nThe parameter " + ParamLine + " is either unrecognized or does not support array values: \n" + str_ParamVal);
							return;
					}
				} catch (Exception Err) {
					MessageBox.Show("In the file " + Program.First_Arg + "\nThe parameter " + ParamLine + " had an invalid value array: \n" + str_ParamVal + "\n" + Err.Message);
				}
			} else {
				if (double.TryParse(str_ParamVal, out dbl_ParamVal)) {
					// parameter value was just a number.
					if (ParamLine.StartsWith("Fema.Mortality_while_") || ParamLine.StartsWith("Male.Mortality_while_")) {
						if (int.TryParse(ParamLine.Substring(21), out i) && i >= 0) {
							if (ParamLine.StartsWith("Male.")) {
								if (i < 1) {
									Set_From_ParamFile(ref Male.Mortality_while_0, 0.0000001, 0.99);
								} else {
									Male.Add_Mortality_Point(i, dbl_ParamVal);
								}
							} else { // female
								if (i < 1) {
									Set_From_ParamFile(ref Fema.Mortality_while_0, 0.0000001, 0.99);
								} else {
									Fema.Add_Mortality_Point(i, dbl_ParamVal);
								}
							}
							return;
						}
						MessageBox.Show("In the file " + Program.First_Arg + "\nThe parameter " + ParamLine + " had an invalid age. It should have the pattern Fema.Mortality_while_40 = 0.01234");
						return;
					}
					switch (ParamLine) {
						case "Iterate_Interval":
							Set_From_ParamFile(ref Iterate_Interval, 0.001, 1);
							return;
						case "Samples_Per_Permutation":
							Set_From_ParamFile(ref Samples_Per_Permutation, 1, 10000);
							return;
						case "MS_Between_Iteration_Breaks":
							Set_From_ParamFile(ref MS_Between_Iteration_Breaks, 1, 100000);
							return;
						case "Initial_Couples":
							Set_From_ParamFile(ref Initial_Couples, 1, 1000);
							return;
						case "Minimum_Gestation":
							Set_From_ParamFile(ref Minimum_Gestation, 0.1, 10);
							return;
						case "Initial_Annual_Pregnancy_Probability":
							Set_From_ParamFile(ref Initial_Annual_Pregnancy_Probability, 0.0001, 0.9999);
							return;
						case "CB_Years_at_Init_Pregnancy_Probability":
							Set_From_ParamFile(ref CB_Years_at_Init_Pregnancy_Probability, 0, 1000);
							return;
						case "Death_in_Childbirth_Probability":
							Set_From_ParamFile(ref Death_in_Childbirth_Probability, 0, 0.999);
							return;
						case "Death_Bearing_Twins_Probability":
							Set_From_ParamFile(ref Death_Bearing_Twins_Probability, 0, 1);
							return;
						case "Twin_Probability":
							Set_From_ParamFile(ref Twin_Probability, 0, 1);
							return;
						case "Probability_Baby_is_Male":
							Set_From_ParamFile(ref Probability_Baby_is_Male, 0.01, 0.99);
							return;
						case "Stop_Iterating_After_Year":
							Set_From_ParamFile(ref Stop_Iterating_After_Year, 1, 100000);
							return;
						case "Stop_Iterating_After_Pop":
							Set_From_ParamFile(ref Stop_Iterating_After_Pop, 10, 10000000);
							return;
						case "First_Marriage_Probability_Per_Year":
							Set_From_ParamFile(ref First_Marriage_Probability_Per_Year, 0.0001, 0.9999);
							return;
						case "Second_Marriage_Probability_Per_Year":
							Set_From_ParamFile(ref Second_Marriage_Probability_Per_Year, 0.0001, 0.9999);
							return;
						case "Third_Plus_Marriage_Probability_Per_Year":
							Set_From_ParamFile(ref Third_Plus_Marriage_Probability_Per_Year, 0.0001, 0.9999);
							return;
						case "Max_Wives":
							Set_From_ParamFile(ref Max_Wives, 1, 1000);
							return;
						case "Man_Too_Old_to_Marry":
							Set_From_ParamFile(ref Man_Too_Old_to_Marry, 30, 9999);
							return;
						case "Fema.Max_Age":
							Set_From_ParamFile(ref Fema.Max_Age, 20, 2000);
							return;
						case "Male.Max_Age":
							Set_From_ParamFile(ref Male.Max_Age, 20, 2000);
							return;
						case "Fema.Min_CBA":
							Set_From_ParamFile(ref Fema_Min_CBA_Plan, 5, 100);
							return;
						case "Fema.Max_CBA":
							Set_From_ParamFile(ref Fema_Max_CBA_Plan, 10, 2000);
							return;
						case "Male.Min_CBA":
							Set_From_ParamFile(ref Male_Min_CBA_Plan, 5, 100);
							return;
						case "Male.Max_CBA":
							Set_From_ParamFile(ref Male_Max_CBA_Plan, 10, 2000);
							return;
						case "Min_Gap_Between_Children":
							Set_From_ParamFile(ref Min_Gap_Between_Ch_Plan, 0.8, 500);
							return;
						default:
							MessageBox.Show("In the file " + Program.First_Arg + "\nThe parameter " + ParamLine + " is either unrecognized. Value:\n" + str_ParamVal);
							return;
					}
				} else {
					MessageBox.Show("In the file " + Program.First_Arg + "\nThe parameter " + ParamLine + " had an invalid value: \n" + str_ParamVal);
				}
			}
		}
		public static string GetNextSegment(ref string s, string Delimeter) {
			if (s == null || s.Length < 1) {
				s = "";
				return "";
			}
			string r;
			int i = s.IndexOf(Delimeter);
			if (i < 0) { // Delimeter not found in string. Return the string and clear it out.
				r = s;
				s = "";
			} else { // Delimeter found. return the portion before the Delimeter and remove it & the Delimeter from s.
				r = s.Substring(0, i);
				s = s.Substring(i + Delimeter.Length);
			}
			return r;
		}
		private static bool Truncate_Line_After(string TruncAfter) {
			// if TruncAfter is found in the string, truncates starting there. returns true if that was the first thing in the string.
			i = ParamLine.IndexOf(TruncAfter); // truncate comments.
			if (i == 0) return true; // entire line was a comment.
			if (i > 0) ParamLine = ParamLine.Substring(0, i);
			return false;
		}
		private static bool Trim_Spaces(ref string ParamLine, string TruncAfter) {
			// if TruncAfter is found in the string, truncates starting there. returns true if that was the first thing in the string.
			i = ParamLine.IndexOf(TruncAfter); // truncate comments.
			if (i == 0) return true; // entire line was a comment.
			if (i > 0) ParamLine = ParamLine.Substring(0, i);
			return false;
		}
		private static void Set_From_ParamFile(ref double ProgParam, double vMin, double vMax) {
			// overload to set double value
			if (Set_From_Param_In_Limits(vMin, vMax)) ProgParam = dbl_ParamVal;
		}
		private static void Set_From_ParamFile(ref double[] ProgParam, double vMin, double vMax) {
			// overload to configure double array to this single value
			if (Set_From_Param_In_Limits(vMin, vMax)) ProgParam =  new double[]{ dbl_ParamVal };				
		}
		private static void Set_From_ParamFile(ref int ProgParam, double vMin, double vMax) {
			// overload to set integer value
			if (Set_From_Param_In_Limits(vMin, vMax)) ProgParam = (int)dbl_ParamVal;			
		}
		private static bool Set_From_Param_In_Limits(double vMin, double vMax) {
			// returns true if in limits, or false if not.
			if (dbl_ParamVal < vMin || dbl_ParamVal > vMax) {
				MessageBox.Show("In the file " + Program.First_Arg + "\nThe parameter " + ParamLine + " had an invalid value: \n" + str_ParamVal + "\nIt needed to be in the range of " + vMin.ToString() + " to " + vMax.ToString());
				return false;
			} else {
				return true;
			}
		}
		#endregion
		#region "Global Routines"
		public static int Higher_of(int a, int b) { // returns the higher of the two integers.
			if (a > b) return a; else return b;
		}
		public static int Lower_of(int a, int b) { // returns the lower of the two integers.
			if (a < b) return a; else return b;
		}
		public static void Re_Init() {
			// resets variables for the next sample run.
			int n;
			double p;
			double m;

			xPop_at_Date = 0;
			xDate_at_Pop = 0;
			xPer_Date_Tots = 0;
			Households.Clear();
			Widows_PCBA.Clear();
			Girls.Clear();
			Boys.Clear();
			Available_Ladies.Clear();
			Iter_Date = 0;
			Iter_Count = 0;
			Total_Pop = Initial_Couples * 2;
			Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Pop_At_Iter_Count_Set();

			// set the parameters which depend on the permutation
			
			Fema.Min_CBA = Fema_Min_CBA_Plan[xCBA_Plan];
			if (Offset_Fema_Max_CBA_from_Min_CBA > 0) {
				Fema.Max_CBA = Fema.Min_CBA + Offset_Fema_Max_CBA_from_Min_CBA;
			} else {
				Fema.Max_CBA = Set_Param_From_Array(ref Fema_Max_CBA_Plan, xCBA_Plan);
			}
			if (Set_Male_Min_CBA_to_Fema_Min_CBA) {
				Male.Min_CBA = Fema.Min_CBA;
			} else {
				Male.Min_CBA = Set_Param_From_Array(ref Male_Min_CBA_Plan, xCBA_Plan);
			}
			if (Set_Male_Max_CBA_to_Max_Age) {
				Male.Max_CBA = Male.Max_Age;
			} else if (Set_Male_Max_CBA_to_Fema_Max_CBA) {
				Male.Max_CBA = Fema.Max_CBA;
			} else {
				Male.Max_CBA = Set_Param_From_Array(ref Male_Max_CBA_Plan, xCBA_Plan);
			}
			Min_Gap_Between_Children = Min_Gap_Between_Ch_Plan[xMin_Gap_Between_Ch_Plan];
			// MessageBox.Show(Fema.Min_CBA.ToString() + " to " + Fema.Max_CBA.ToString() + " <-- Female    Male --> " + Male.Min_CBA.ToString() + " to " + Male.Max_CBA.ToString()+ "\n" +Min_Gap_Between_Children.ToString());

			if (Permutation_Sample_Num == 0) {
				// we are taking the first sample. Capture the permutations header.
				PermHeader1.Append("," + Fema.Min_CBA.ToString(DateFormat));
				PermHeader2.Append("," + Fema.Max_CBA.ToString(DateFormat));
				PermHeader3.Append("," + Male.Min_CBA.ToString(DateFormat));
				PermHeader4.Append("," + Male.Max_CBA.ToString(DateFormat));
				PermHeader5.Append("," + Min_Gap_Between_Children.ToString(DateFormat));
				if (xMin_Gap_Between_Ch_Plan == 0) {
					// it's also the first set of gaps. capture the summary header.
					SummaryHeader1.Append("," + Fema.Min_CBA.ToString(DateFormat));
					SummaryHeader2.Append("," + Fema.Max_CBA.ToString(DateFormat));
					SummaryHeader3.Append("," + Male.Min_CBA.ToString(DateFormat));
					SummaryHeader4.Append("," + Male.Max_CBA.ToString(DateFormat));
				}
			}
			/* Pregnancy Probability:
			 * In modern western cultures, pregnancy probability starts high (0.88 annually for women trying to get pregnant)
			 * and stays level until the age of 25. It then begins to drop slowly, exponentially increasing until a drastic
			 * drop to a low of 0.04 at the age of 46, where it flattens back out. For simplicity, we will drop all the way
			 * to zero at Max_CBA.		 */

			Pregnancy_Probability_perIter = new double[(int)Fema.Max_CBA + 2];
			n = (int)Fema.Min_CBA;
			// for the first few years of child-bearing age, the pregnancy probability stays high.
			p = 1 - Initial_Annual_Pregnancy_Probability;
			for (i = 0; i < CB_Years_at_Init_Pregnancy_Probability; i++) {
				Pregnancy_Probability_perIter[n++] = Probability_PerIter_from_PerYear(1 - p);
				/* if we just filled out the childbearing year, bail out of this for loop.
				 * that would happen if there is a permutation in which the Fema.Min_CBA
				 * is closer to Fema.Max_CBA than CB_Years_at_Init_Pregnancy_Probability. */
				if (n >= Pregnancy_Probability_perIter.Length) i = (int)CB_Years_at_Init_Pregnancy_Probability + 2;
			}
			// then exponentially drop the probability to 0 at the max childbearing age.
			// do this by tracking the probability of NOT getting pregnant, which will increase exponentially every year.
			m = 1 / (Math.Pow(p, 1 / (Fema.Max_CBA + 1 - Fema.Min_CBA - CB_Years_at_Init_Pregnancy_Probability)));
			while (n < Pregnancy_Probability_perIter.Length) {
				Pregnancy_Probability_perIter[n++] = Probability_PerIter_from_PerYear(1 - p);
				p *= m;
			}
/*			this is for debugging only:
 			Result_Ext.Length = 0;
			for (n = 0; n < Pregnancy_Probability_perIter.Length; n++) {
				Result_Ext.Append("\n" + n.ToString() + "     " + Pregnancy_Probability_perIter[n].ToString("0.000000"));
			}
			MessageBox.Show("Age    Pregnancy Probability" + Result_Ext.ToString()); */

			for (i = 0; i < Initial_Couples; i++) Households.Add(1);

			Debug_Trace.Length = 0;
			n = (int)(Stop_Iterating_After_Pop * (0.02 + Probability_Baby_is_Male));
			for (i = 0; i < n; i++) Debug_Trace.Append("," + i.ToString());
			Debug_Trace_Blank_Len = Debug_Trace.Length;
			Peak_Pop = 0;
			Track_Babies = true;
		}
		private static double Set_Param_From_Array(ref double[] ParamArray, int ArrayIndex) {
			// returns the array element indicate by the index, or the last element if the index is past the end.
			if (ArrayIndex >= ParamArray.Length) return ParamArray[ParamArray.Length - 1];
			return ParamArray[ArrayIndex];
		}
		public static double Probability_PerIter_from_PerYear(double Probability_Per_Year) {
			/*		Calculate a per-iteration probability from an annual probability, so that when scanned every
			 *		iteration, the event happens the number of times per year indicated by the annual probability.
			 *		Example: for a 0.9 (90%) probability of a CBA wife giving birth in a year, the probability per
			 *		iteration should be 0.2057 (20.6%) if iterating every 0.1 years.
			 *		We want to calculate the per-iteration probability now (at the beginning) so we can just look
			 *		it up without having to do any advanced calculation while iterating.
			 *
			 *		The 1- pieces in the calculation indicate we are really dealing with the probability of the
			 *		event NOT happening in a given year. For the example above, that is a 0.1 (10%) probability
			 *		of the woman not having a child. Multiplying the per-iteration probability of not having a
			 *		child (0.7943, or 79.4%) by itself 10 times yields 0.1.   */
			return 1 - Math.Pow(1 - Probability_Per_Year, Iterate_Interval);
		}
		public static bool Male_Died(int iAge) {
			// returns true if a male of the specified age died.
			return (RandGen.NextDouble() < Male.ActuarialTable[iAge].PerIter);
		}
		public static bool Fema_Died(int iAge) {
			// returns true if a female of the specified age died.
			return (RandGen.NextDouble() < Fema.ActuarialTable[iAge].PerIter);
		}
		public static bool Guy_Gets_Girl(double Man_Age, int Wife_Count) {
			// returns true if this guy - based on his age and the number of wives he already has - gets married today.
			if (Man_Age > Man_Too_Old_to_Marry) return false;
			if (Wife_Count >= Max_Wives) return false; // he's already married to enough wives. 
			// if we got here, he's not too old to marry and doesn't have too many wives.
			if (Available_Ladies.Count <= 0) return false; // no ladies available for marriage ... no bride for him!
			// pick a particular available lady at random and see if they should get married.
			xAvailLady = (int)(Available_Ladies.Count * RandGen.NextDouble());
			// see how old that random available lady is.
			Wife_Age = Iter_Date - Available_Ladies.Individuals[xAvailLady].BirthDate;
			dAgeDif = 100.0 * (Man_Age - Wife_Age) / (Man_Age + Wife_Age);
			//~~~~~~~~~ consider desperation & husb/wife age difference here.
			if (RandGen.NextDouble() < Marriage_Probability_Per_Iter[Wife_Count]) {
				Wives_Count++;
				// accumulate the age difference distribution.
				Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Accumulate_Marriage_By_AgeDif(100+(int)dAgeDif);
				return true;
			} else {
				return false;
			}
		}
		public static bool Have_a_Baby(int Age, double Min_Next_Child) {
			// tells if this wife had a boy and/or girl. returns true if something happened.

			// see if it's been long enough since she got married or had the last child to have another one.
			if (Iter_Date < Min_Next_Child) return false; // not long enough yet.

			if (RandGen.NextDouble() > Pregnancy_Probability_perIter[Age]) return false; // no baby.

			if (RandGen.NextDouble() < Twin_Probability) {
				// having twins. for programming simplicity, twins will alway be a boy and a girl.
				if (RandGen.NextDouble() < Death_Bearing_Twins_Probability) {
					Childbirth_Mother_Died = true; // died in childbirth 
				} else {
					// twins and everyone is healthy.
					Childbirth_Mother_Died = false;
					Childbirth_Boy = true;
					Childbirth_Girl = true;
				}
			} else if (RandGen.NextDouble() < Death_in_Childbirth_Probability) {
				// died in childbirth.
				Childbirth_Mother_Died = true;
			} else {
				Childbirth_Mother_Died = false;
				if (RandGen.NextDouble() < Probability_Baby_is_Male) {
					Childbirth_Boy = true;
					Childbirth_Girl = false;
				} else {
					Childbirth_Boy = false;
					Childbirth_Girl = true;
				}
			}
			return true; // something interesting happened (new child or dead mother)
		}
		public static void Execute_Iterations() {
			Already_Executing = true;
			if (Iterate_Slowly) {
				// just do the one iteration per tick.
				Take_a_Break = DateTime.Now.AddMilliseconds(-1);
			} else {
				Take_a_Break = DateTime.Now.AddMilliseconds(MS_Between_Iteration_Breaks);
			}
			do {
				Iterate_People();
			} while (Still_Running && DateTime.Now <= Take_a_Break);

			ElapsedTime = DateTime.Now.Subtract(StartTime);
			str_ElapsedTime = ElapsedTime.ToString();
			i = str_ElapsedTime.LastIndexOf(".");
			if (i > 2) str_ElapsedTime = str_ElapsedTime.Substring(0, i + 2);
			Already_Executing = false;
		}
		private static void Iterate_People() {
			Iter_Date += Iterate_Interval;
			Iter_Count++;

			// scan in reverse order of promotion so we don't incorrectly double the odds of someone dying when promoted.
			Widows_PCBA.Scan_Widows_PCBA();
			Available_Ladies.Scan_Available_Ladies();
			Households.Scan_Households();
			Boys.Scan_Boys();
			Girls.Scan_Girls();

			Total_Pop = Households.Count + Wives_Count + Girls.Count + Boys.Count + Available_Ladies.Count + Widows_PCBA.Count;
			// Peak_Pop will be the same as Total_Pop for a growing population, but is interesting for populations that go extinct.
			if (Peak_Pop < Total_Pop) Peak_Pop = Total_Pop;
			if (xPop_at_Date < Report_Pop_at_Date.Length && Iter_Date > Report_Pop_at_Date[xPop_at_Date]) {
				// we just reached a date where we should capture the population.
				Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Sample[Permutation_Sample_Num].Pop_at_Date[xPop_at_Date++] = Total_Pop;
			}
			if (xDate_at_Pop < Report_Date_at_Pop.Length && Total_Pop > Report_Date_at_Pop[xDate_at_Pop]) {
				// we just reached a population where we should capture the date at which it was reached.
				Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Sample[Permutation_Sample_Num].Date_at_Pop[xDate_at_Pop++] = Iter_Date;
			}

			if (Output_Debug_Trace) {
				Debug_Trace.Append("\nDate=" + Iter_Date.ToString(DateFormat) + "  Total Pop=" + Total_Pop.ToString());
				Girls.Debug_Trace_Summary("Girls");
				Boys.Debug_Trace_Summary("Boys");
				Available_Ladies.Debug_Trace_Summary("Available Ladies");
				Widows_PCBA.Debug_Trace_Summary("Widows PCBA");
				Households.Debug_Trace_Summary();
			}
			if (Track_Babies) { // stop looking for the target after we already reached it.
				Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Pop_At_Iter_Count_Set();
				if (xPer_Date_Tots < Per_Date_Tots.Length) {
					Per_Date_Tots[xPer_Date_Tots].Iter_Date = Iter_Date;
					Per_Date_Tots[xPer_Date_Tots++].Tot_Pop = Total_Pop;
				}
				if (Iter_Date > Stop_Iterating_After_Year || Total_Pop > Stop_Iterating_After_Pop || Total_Pop < 1) {
					/* we have reached the target number of years or population. capture the relvant values, then stop tracking
					 * anyone born after this point. The simulation will continue to run until the last baby alive at this time
					 * has died, at which point all the distributions can be calculated.				*/
					struct_Person.xGens_Rem_Dist = 0;
					Boys.Capture_Gens_Rem_Dist();
					Girls.Capture_Gens_Rem_Dist();
					Available_Ladies.Capture_Gens_Rem_Dist();
					Widows_PCBA.Capture_Gens_Rem_Dist();
					Households.Capture_Gens_Rem_Dist();
					Track_Babies = false;
					if (Total_Pop > 20) { //  <-- division by zero protection.
						Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Get_Age_Dist();
						// go back through the dates and find the last date when the population was <1/10 what it is now.
						int Tenth_Pop = Total_Pop / 10;
						while (xPer_Date_Tots-- > 3 && Per_Date_Tots[xPer_Date_Tots].Tot_Pop > Tenth_Pop) { }
						Term_Growth_Rate = 0; // assume no valid growth rate unless everything checks out.
						if (xPer_Date_Tots > 5 && Per_Date_Tots[xPer_Date_Tots].Tot_Pop > 1) {
							/* calculate the terminal growth rate by comparing the population now to the population when at 1/10
							 *	of the final target. Use the classic growth rate equation:		n = n'*e^(kt)
							 *	solved for k:																	k = ln(n/n')/t    	*/
							Term_Growth_Rate = Math.Log((double)Total_Pop / (double)Per_Date_Tots[xPer_Date_Tots].Tot_Pop)
															/ (Iter_Date - Per_Date_Tots[xPer_Date_Tots].Iter_Date);
							// If the population hit its max before some of the date capture points were reached, project the 
							// population at each date based on the terminal growth rate instead of by continuing to iterate.
							while (xPop_at_Date < Report_Pop_at_Date.Length) {
								// use the base growth equation above:		n = n'*e^(kt)
								Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Sample[Permutation_Sample_Num].Pop_at_Date[xPop_at_Date] =
									((double)Total_Pop) * Math.Exp(Term_Growth_Rate * (Report_Pop_at_Date[xPop_at_Date] - Iter_Date));
								xPop_at_Date++;
							}
							// If the date hit its max before some of the population capture points were reached, project the 
							// date for each population point based on the terminal growth rate instead of by continuing to iterate.
							while (xDate_at_Pop < Report_Date_at_Pop.Length) {
								// the growth equation above, solved for time:   t = ln(n/n')/k
								Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Sample[Permutation_Sample_Num].Date_at_Pop[xDate_at_Pop] =
									Iter_Date + Math.Log(Report_Date_at_Pop[xDate_at_Pop] / Total_Pop) / Term_Growth_Rate;
								xDate_at_Pop++;
							}
						}
					}
					Results[xCBA_Plan, xMin_Gap_Between_Ch_Plan].Capture_Target_Data();
				}
			}
			if (Total_Pop < 1) {
				/* done with this run, either because it went prematurely extinct, or (normally) because we stopped tracking
				 * babies when a target was reached, and all the people alive at that time have died. */
				Wrap_Up_Sample_Run();
			}
		}
		private static void Wrap_Up_Sample_Run() {
			/* done with a sample run. Move to the next permutation. When all the permutations have been sampled, go to the
			 * next sample number and do it all over again until we have completed the requested number of samples. Doing
			 *	one sample of each permutation at a time has these advantage:
			 *	1. Sample run time heavily depends on the number of years to reach the target, so some permutations take a lot
			 *		longer to iterate than others. Doing one sample of all permutations at a time allows the program to accurately
			 *		predict how long it will take to complete from the very beginning.
			 *	2.	Patterns can be seen much earlier from the average of just a few samples.
			 *	3. Since the results will be published after all permutations in a sample are complete, if the program is 
			 *		interrupted before all samples are completed, there may still be valuable output data available.		 */

			if (Debug_Trace.Length > Debug_Trace_Blank_Len) {
				File_Create_Dir(Results_Path);
				File_Write_Text(Results_Path + "debug_trace_" + Permutation_Sample_Num.ToString() + "_" + Fema.Min_CBA.ToString("0") +
							"_" + Min_Gap_Between_Children.ToString("00.0").Replace(".", "") + ".csv", Debug_Trace.ToString());
			}

			if (Pause_After_Run > 0) Pause_After_Run = 2;
			Samples_Run++; // for the progress bar
			dbl_PctDone = Samples_Run / Total_Samples;
			str_PctDone = dbl_PctDone.ToString("0.00%");
			ElapsedTime = DateTime.Now.Subtract(StartTime);
			// estimate the end time by assuming the % runs completed is the % time completed.
			EstEndTime = StartTime.AddTicks((long)((double)ElapsedTime.Ticks / dbl_PctDone));
			str_Progress = EstEndTime.Subtract(DateTime.Now).ToString();
			int i = str_Progress.LastIndexOf(".");
			if (i > 4) str_Progress = str_Progress.Substring(0, i);
			str_Progress = " elapsed  " + Samples_Run.ToString("#,0") + " / " + Total_Samples.ToString("#,0") + " samples ("
					+ str_PctDone + ")   estimated " + str_Progress + " left, ending at " + EstEndTime.ToString("MMM d HH:mm");

			// move to the next minimum child-bearing age permutation
			if (++xCBA_Plan >= Fema_Min_CBA_Plan.Length) {
				// done with that series of minimum childbearing age permutations.
				// go back to the beginning of Min-CBA and move to the next permutation for gap between children.
				xCBA_Plan = 0;
				if (Permutation_Sample_Num == 0 && xMin_Gap_Between_Ch_Plan == 0) {
					// we just finished the first run through the child-bearing age permutations. construct the summary table header.
					table_header = SummaryHeader1.ToString() + SummaryHeader2.ToString() + SummaryHeader3.ToString() + SummaryHeader4.ToString() + ",<-- Male Maximum";
					// now that we've constructed the summaryheader text, the memory from the StringBuilders can be released.
					Release_StringBuilder_Mem(SummaryHeader1);
					Release_StringBuilder_Mem(SummaryHeader2);
					Release_StringBuilder_Mem(SummaryHeader3);
					Release_StringBuilder_Mem(SummaryHeader4);
				}
				if (++xMin_Gap_Between_Ch_Plan >= Min_Gap_Between_Ch_Plan.Length) {
					// done with all permutations for this sample.
					// go back to the beginning and do them all over again for the next sample. Also publish the results so far.
					xMin_Gap_Between_Ch_Plan = 0;
					if (Permutation_Sample_Num == 0) {
						// this is the end of the first sample. We now have the entire permutations header. construct it.
						Permutation_header = PermHeader1.ToString() + PermHeader2.ToString() + PermHeader3.ToString()
								+ PermHeader4.ToString() + ",Actuarial Plan"
								+ PermHeader5.ToString() + ",/Year,/Iter";
						// we won't be needing the rest again - free that memory.
						Release_StringBuilder_Mem(PermHeader1);
						Release_StringBuilder_Mem(PermHeader2);
						Release_StringBuilder_Mem(PermHeader3);
						Release_StringBuilder_Mem(PermHeader4);
						Release_StringBuilder_Mem(PermHeader5);
					}
					Permutation_Sample_Num++;
					Prepare_to_Publish_Results();
					if (Permutation_Sample_Num >= Samples_Per_Permutation) {
						// Sample count has reached its target, so we are totally done.
						Still_Running = false;
						return;
					}
				}
			}
			Re_Init();
		}
		private static void Release_StringBuilder_Mem(StringBuilder sb) {
			sb.Length = 0;
			sb.Capacity = 1;
		}
		private static void Prepare_to_Publish_Results() {
			/* Produces the following CSV (comma separated variable) files in the directory for this run:
			 *		Sample_Summary.CSV
			 *			This has columns for description, and each permutation of Min-CBA and child-gap.
			 *			Rows include distributions for age at death for males & females, number of children,
			 *			peak number of wives for men, and ages of males & females at the target time.
			 *			Rows also include total extinctions, years to target, terminal growth rate,
			 *			peak population for each sample and an overall average. 
			 *		Grand_Averages.CSV
			 *			This has a list of the parameter values, and grids for averages of years to target,
			 *			terminal growth rate, and samples resulting in extinction for each permutation.  */
			int xMCBA; // for indexing through minimum child-bearing age permutations
			int xYBCB; // for indexing through years between child-birth permutations
			int xSamp;
			int n;
			double Years_to_Max_Pop;
			double Term_Growth_Rate;
			double Peak_Population;
			string SamplNum;
			bool Keep_Looping;
			bool Averages_Done;
			bool Minimums_Done;
			bool Found_Average;
			bool Found_Minimum;

			Result_SampDet.Length = 0;
			Result_YTT.Length = 0;
			Result_NETGR.Length = 0;
			Result_TATGR.Length = 0;
			Result_Ext.Length = 0;
			Result_PkP.Length = 0;
			Dead_Boys.Length = 0;
			Dead_Girls.Length = 0;
			Dead_Available_Ladies.Length = 0;
			Dead_Widows_PCBA.Length = 0;
			Dead_Men.Length = 0;
			Dead_Wives_by_Age.Length = 0;
			Dead_Wives_by_ChildBirth.Length = 0;
			Result_YTT.Append("\nYears to Target Avg,");
			Result_TATGR.Append("\nAverage Terminal Growth Rate for All Samples,");
			Result_NETGR.Append("\nAverage Terminal Growth Rate for Non-Extinct Samples,");
			Result_PkP.Append("\nPeak Iterated Population Avg,");
			Result_Ext.Append("\nSamples Ending in Extinction and/or insufficient end-population to calculate growth rate,");
			Dead_Boys.Append("\nDead Boys,");
			Dead_Girls.Append("\nDead Girls,");
			Dead_Available_Ladies.Append("\nDead Available Ladies,");
			Dead_Widows_PCBA.Append("\nDead Widows Past Childbearing Age,");
			Dead_Men.Append("\nDead Men,");
			Dead_Wives_by_Age.Append("\nDead Wives by Age/Actuarial,");
			Dead_Wives_by_ChildBirth.Append("\nDead Wives by Child Birth,");
			for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
				for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
					// start by clearing averages.
					Years_to_Max_Pop = 0;
					Term_Growth_Rate = 0;
					Peak_Population = 0;
					n = 0;
					// run through all available samples and accumulate the ones that didn't go extinct.
					for (xSamp = 0; xSamp < Permutation_Sample_Num; xSamp++) {
						Peak_Population += Results[xMCBA, xYBCB].Sample[xSamp].Peak_Population;
						if (Results[xMCBA, xYBCB].Sample[xSamp].Terminal_Growth_Rate > 0) {
							n++;
							Years_to_Max_Pop += Results[xMCBA, xYBCB].Sample[xSamp].Years_to_Max_Pop;
							Term_Growth_Rate += Results[xMCBA, xYBCB].Sample[xSamp].Terminal_Growth_Rate;
						}
					}
					Results[xMCBA, xYBCB].SamplAvg.Peak_Population = (int)(Peak_Population / Permutation_Sample_Num);
					Results[xMCBA, xYBCB].Tot_Avg_Term_Growth_Rate = (Term_Growth_Rate / Permutation_Sample_Num);
					if (n > 0) {
						Results[xMCBA, xYBCB].SamplAvg.Years_to_Max_Pop = Years_to_Max_Pop / n;
						Results[xMCBA, xYBCB].SamplAvg.Terminal_Growth_Rate = Term_Growth_Rate / n;
					}
					Results[xMCBA, xYBCB].Append_StringBuilders();
				}
			}
			for (xSamp = 0; xSamp < Permutation_Sample_Num; xSamp++) {
				SamplNum = ": Sample #," + (xSamp + 1).ToString();
				Result_YTT.Append("\nYears to Target" + SamplNum);
				Result_TATGR.Append("\nTerminal Growth Rate" + SamplNum);
				Result_PkP.Append("\nPeak Iterated Population" + SamplNum);
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Result_YTT.Append("," + Results[xMCBA, xYBCB].Sample[xSamp].Years_to_Max_Pop.ToString());
						Result_TATGR.Append("," + Results[xMCBA, xYBCB].Sample[xSamp].Terminal_Growth_Rate.ToString());
						Result_PkP.Append("," + Results[xMCBA, xYBCB].Sample[xSamp].Peak_Population.ToString());
					}
				}
			}
			Result_SampDet.Append(Permutation_header);
			Result_SampDet.Append(Result_Ext.ToString());
			Result_SampDet.Append(Result_YTT.ToString());
			Result_SampDet.Append(Result_NETGR.ToString());
			Result_SampDet.Append(Result_TATGR.ToString());
			Result_SampDet.Append(Result_PkP.ToString());

			for (xDate_at_Pop = 0; xDate_at_Pop < Report_Date_at_Pop.Length; xDate_at_Pop++) {
				// clear average counts.
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Results[xMCBA, xYBCB].n = 0;
						Results[xMCBA, xYBCB].SamplAvg.Date_at_Pop[xDate_at_Pop] = 0;
					}
				}
				Years_to_Max_Pop = 0;
				SamplNum = "\nDate when Population reaches " + Report_Date_at_Pop[xDate_at_Pop].ToString();
				for (xSamp = 0; xSamp < Permutation_Sample_Num; xSamp++) {
					Result_SampDet.Append(SamplNum + "," + xSamp.ToString());
					for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
						for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
							Result_SampDet.Append("," + Results[xMCBA, xYBCB].Sample[xSamp].Date_at_Pop[xDate_at_Pop].ToString(DateFormat));
							if (Results[xMCBA, xYBCB].Sample[xSamp].Date_at_Pop[xDate_at_Pop] > 0) {
								Results[xMCBA, xYBCB].n++;
								Results[xMCBA, xYBCB].SamplAvg.Date_at_Pop[xDate_at_Pop] += Results[xMCBA, xYBCB].Sample[xSamp].Date_at_Pop[xDate_at_Pop];
							}
						}
					}
				}
				Result_SampDet.Append(SamplNum + " Average,");
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						if (Results[xMCBA, xYBCB].n > 0) {
							Results[xMCBA, xYBCB].SamplAvg.Date_at_Pop[xDate_at_Pop] /= Results[xMCBA, xYBCB].n;
						}
						Result_SampDet.Append("," + Results[xMCBA, xYBCB].SamplAvg.Date_at_Pop[xDate_at_Pop].ToString(DateFormat));
					}
				}
			}

			for (xPop_at_Date = 0; xPop_at_Date < Report_Pop_at_Date.Length; xPop_at_Date++) {
				// clear average counts.
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Results[xMCBA, xYBCB].n = 0;
						Results[xMCBA, xYBCB].SamplAvg.Pop_at_Date[xPop_at_Date] = 0;
					}
				}
				Years_to_Max_Pop = 0;
				SamplNum = "Population After " + Report_Pop_at_Date[xPop_at_Date].ToString() + " Years,";
				for (xSamp = 0; xSamp < Permutation_Sample_Num; xSamp++) {
					Result_SampDet.Append("\n" + SamplNum + xSamp.ToString());
					for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
						for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
							Result_SampDet.Append("," + Results[xMCBA, xYBCB].Sample[xSamp].Pop_at_Date[xPop_at_Date].ToString("0"));
							if (Results[xMCBA, xYBCB].Sample[xSamp].Pop_at_Date[xPop_at_Date] > 0) {
								Results[xMCBA, xYBCB].n++;
								Results[xMCBA, xYBCB].SamplAvg.Pop_at_Date[xPop_at_Date] += Results[xMCBA, xYBCB].Sample[xSamp].Pop_at_Date[xPop_at_Date];
							}
						}
					}
				}
				Result_SampDet.Append("\nAverage " + SamplNum);
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						if (Results[xMCBA, xYBCB].n > 0) {
							Results[xMCBA, xYBCB].SamplAvg.Pop_at_Date[xPop_at_Date] /= Results[xMCBA, xYBCB].n;
						}
						Result_SampDet.Append("," + Results[xMCBA, xYBCB].SamplAvg.Pop_at_Date[xPop_at_Date].ToString("0"));
					}
				}
			}

			Result_SampDet.Append(Dead_Girls.ToString());
			Result_SampDet.Append(Dead_Available_Ladies.ToString());
			Result_SampDet.Append(Dead_Widows_PCBA.ToString());
			Result_SampDet.Append(Dead_Wives_by_Age.ToString());
			Result_SampDet.Append(Dead_Wives_by_ChildBirth.ToString());

			for (xSamp = 0; xSamp <= struct_Results.Max_Child_Per_Mother; xSamp++) {
				Result_SampDet.Append("\nFemales Who Died After Bearing " + xSamp.ToString() + " Children," + xSamp.ToString());
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						if (xSamp < Results[xMCBA, xYBCB].Children_Per_Mother.Length) {
							Result_SampDet.Append("," + Results[xMCBA, xYBCB].Children_Per_Mother[xSamp].ToString());
						} else {
							Result_SampDet.Append(",0");
						}
					}
				}
			}
			for (xSamp = 0; xSamp <= struct_Results.Max_Marriages_Per_Woman; xSamp++) {
				Result_SampDet.Append("\nFemles Who Died After Marrying " + xSamp.ToString() + " Husbands," + xSamp.ToString());
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						if (xSamp < Results[xMCBA, xYBCB].Marriages_Per_Wife.Length) {
							Result_SampDet.Append("," + Results[xMCBA, xYBCB].Marriages_Per_Wife[xSamp].ToString());
						} else {
							Result_SampDet.Append(",0");
						}
					}
				}
			}
			for (xSamp = struct_Results.Min_Marriage_AgeDif; xSamp <= struct_Results.Max_Marriage_AgeDif; xSamp++) {
				if (xSamp > 99) { // male older
					Result_SampDet.Append("\nMarriages With Man " + (xSamp - 100).ToString() + "% Older than Their Avg Age," + (xSamp - 100).ToString());
				} else { // female older
					Result_SampDet.Append("\nMarriages With Woman " + (99 - xSamp).ToString() + "% Older than Their Avg Age," + (xSamp - 100).ToString());
				}
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Result_SampDet.Append("," + Results[xMCBA, xYBCB].Marriages_Per_AgeDif[xSamp].ToString());
					}
				}
			}
			Result_SampDet.Append(Dead_Boys.ToString());
			Result_SampDet.Append(Dead_Men.ToString());
			for (xSamp = 0; xSamp <= struct_Results.Max_Child_Per_Father; xSamp++) {
				Result_SampDet.Append("\nMales Who Died After Fathering " + xSamp.ToString() + " Children," + xSamp.ToString());
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						if (xSamp < Results[xMCBA, xYBCB].Children_Per_Father.Length) {
							Result_SampDet.Append("," + Results[xMCBA, xYBCB].Children_Per_Father[xSamp].ToString());
						} else {
							Result_SampDet.Append(",0");
						}
					}
				}
			}
			Result_SampDet.Append("\nNote that this simulation does NOT account for excess death of males due to occupation and warfare. Therefore 'Males Who Died After Marrying 0 Wives' is a good estimate of the number of males who could be killed in battle without any effect on population growth rate.");
			for (xSamp = 0; xSamp <= struct_Results.Max_Marriages_Per_Man; xSamp++) {
				Result_SampDet.Append("\nMales Who Died After Marrying " + xSamp.ToString() + " Wives," + xSamp.ToString());
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						if (xSamp < Results[xMCBA, xYBCB].Marriages_Per_Man.Length) {
							Result_SampDet.Append("," + Results[xMCBA, xYBCB].Marriages_Per_Man[xSamp].ToString());
						} else {
							Result_SampDet.Append(",0");
						}
					}
				}
			}
			for (n = 1; n <= Max_Wives; n++) { // don't put in a row for 0 peak wives. It's the same as for "died after marrying 0" above.
				Result_SampDet.Append("\nMales With a Peak Wife Count of," + n.ToString());
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Result_SampDet.Append("," + Results[xMCBA, xYBCB].Peak_Wives_Per_Man[n].ToString());
					}
				}
			}
			// actuarial data and live distribution by age:
			Result_YTT.Length = 0;
			for (n = 0; n <= Fema.Max_Age; n++) {
				Result_SampDet.Append("\nFemales Who Died at Age," + n.ToString());
				Result_YTT.Append("\nEnd-of-Run Live Females at Age," + n.ToString());
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Result_SampDet.Append("," + Results[xMCBA, xYBCB].Fema_Deaths[n].ToString());
						Result_YTT.Append("," + Results[xMCBA, xYBCB].Fema_Distrb[n].ToString());
					}
				}
				// also list the actuarial plan for comparison:
				Result_SampDet.Append("," + Fema.ActuarialTable[n].PerYear.ToString() + "," + Fema.ActuarialTable[n].PerIter.ToString());
			}
			Result_SampDet.Append(Result_YTT.ToString());
			Result_YTT.Length = 0;
			for (n = 0; n <= Male.Max_Age; n++) {
				Result_SampDet.Append("\nMales Who Died at Age," + n.ToString());
				Result_YTT.Append("\nEnd-of-Run Live Males at Age," + n.ToString());
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Result_SampDet.Append("," + Results[xMCBA, xYBCB].Male_Deaths[n].ToString());
						Result_YTT.Append("," + Results[xMCBA, xYBCB].Male_Distrb[n].ToString());
					}
				}
				// also list the actuarial plan for comparison:
				Result_SampDet.Append("," + Male.ActuarialTable[n].PerYear.ToString() + "," + Male.ActuarialTable[n].PerIter.ToString());
			}
			Result_SampDet.Append(Result_YTT.ToString());

			// dump the generations-removed distributions to the report file.
			for (n = 0; n < Results[0, 0].Gens_Rem_Dist.Length; n++) {
				Gen_Rem_Dist_Max.Length = 0;
				Gen_Rem_Dist_Min.Length = 0;
				Gen_Rem_Dist_Avg.Length = 0;
				Keep_Looping = true;
				Found_Average = true;
				Found_Minimum = true;
				xSamp = 0;
				while (Keep_Looping) {
					if (Found_Average) Gen_Rem_Dist_Avg.Append(Results[0, 0].Gens_Rem_Dist[n].Desc + "Avg," + xSamp.ToString());
					Gen_Rem_Dist_Max.Append(Results[0, 0].Gens_Rem_Dist[n].Desc + "Max," + xSamp.ToString());
					if (Found_Minimum) Gen_Rem_Dist_Min.Append(Results[0, 0].Gens_Rem_Dist[n].Desc + "Min," + xSamp.ToString());
					Keep_Looping = false; // only go again after this if there are more distribution levels.
					Minimums_Done = !Found_Minimum;
					Averages_Done = !Found_Average;
					Found_Minimum = false;
					Found_Average = false;
					for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
						for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
							if (Results[xMCBA, xYBCB].Gens_Rem_Dist[n].Max.Dist.Length > xSamp ) {
								Gen_Rem_Dist_Max.Append("," + Results[xMCBA, xYBCB].Gens_Rem_Dist[n].Max.Dist[xSamp].ToString());
								Keep_Looping = true;
							} else { // past the end of this one.
								Gen_Rem_Dist_Max.Append(",0");
							}
							if (Averages_Done) {
								// do nothing... no more averages to report.
							} else if (Results[xMCBA, xYBCB].Gens_Rem_Dist[n].Avg.Dist.Length > xSamp) {
								Gen_Rem_Dist_Avg.Append("," + Results[xMCBA, xYBCB].Gens_Rem_Dist[n].Avg.Dist[xSamp].ToString());
								Found_Average = true;
							} else { // past the end of this one.
								Gen_Rem_Dist_Avg.Append(",0");
							}
							if (Minimums_Done) {
								// do nothing... no more minimums to report.
							} else if (Results[xMCBA, xYBCB].Gens_Rem_Dist[n].Min.Dist.Length > xSamp) {
								Gen_Rem_Dist_Min.Append("," + Results[xMCBA, xYBCB].Gens_Rem_Dist[n].Min.Dist[xSamp].ToString());
								Found_Minimum = true;
							} else { // past the end of this one.
								Gen_Rem_Dist_Min.Append(",0");
							}
						}
					}
					xSamp++;
				}
				Result_SampDet.Append(Gen_Rem_Dist_Avg.ToString() + Gen_Rem_Dist_Max.ToString() + Gen_Rem_Dist_Min.ToString());
			}

			// now create a file that shows the population per year for every permutation in this sample.
			Result_PkP.Length = 0;
			Result_PkP.Append(Permutation_header);
			n = -1;
			Keep_Looping = true;
			while (Keep_Looping) {
				Keep_Looping = false; // stop looping unless we find a non-zero population for this year.
				Result_PkP.Append("\nPopulation at Year," + (++n * Iterate_Interval).ToString(DateFormat));
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						if (n < Results[xMCBA, xYBCB].Pop_At_Iter_Count.Length && Results[xMCBA, xYBCB].Pop_At_Iter_Count[n] > 0) {
							Result_PkP.Append("," + Results[xMCBA, xYBCB].Pop_At_Iter_Count[n].ToString());
							Keep_Looping = true; // we found a non-zero population for this year. Keep going.
						} else {
							Result_PkP.Append(",");
						}
					}
				}
			}
			// no reason to remember this for re-publishing later - just dump it and forget it.
			File_Create_Dir(Results_Path);
			File_Write_Text(Results_Path + "Populations_at_Dates_" + Permutation_Sample_Num.ToString("0000") + ".csv", Result_PkP.ToString());





			// now create Grand_Averages.CSV
			Result_YTT.Length = 0;
			Result_TATGR.Length = 0;
			Result_NETGR.Length = 0;
			Result_Ext.Length = 0;
			Result_PkP.Length = 0;
			Result_YTT.Append("Parameter Settings from " + Parameter_File_Name + "\n"
				+ Samples_Per_Permutation.ToString() + ",Samples_Per_Permutation\n"
				+ Permutation_Sample_Num.ToString() + ",Permutation Sample Cycles Completed\n"
				+ str_ElapsedTime + ",Run Time Elapsed\n"
				+ Stop_Iterating_After_Pop.ToString() + ",Stop_Iterating_After_Pop - stop iterating new people when reaching this population\n"
				+ Stop_Iterating_After_Year.ToString() + ",Stop_Iterating_After_Year - stop iterating new people when reaching this date\n"
				+ Initial_Couples.ToString() + ",Initial_Couples\n"
				+ Max_Wives.ToString() + ",Max_Wives at a time per man\n"
				+ Initial_Annual_Pregnancy_Probability.ToString() + ",Initial_Annual_Pregnancy_Probability for women below the age of Min_CBA + ...\n"
				+ CB_Years_at_Init_Pregnancy_Probability.ToString() + ",CB_Years_at_Init_Pregnancy_Probability - years after Min_CBA at which pregnancy probability begins dropping exponentially to 0 at Max_CBA\n"
				+ Death_in_Childbirth_Probability.ToString() + ",Death_in_Childbirth_Probability (per child)\n"
				+ Death_Bearing_Twins_Probability.ToString() + ",Death_Bearing_Twins_Probability (per pregnancy)\n"
				+ Twin_Probability.ToString() + ",Twin_Probability (per pregnancy)\n"
				+ Probability_Baby_is_Male.ToString() + ",Probability_Baby_is_Male\n"
				+ Iterate_Interval.ToString() + ",Iterate_Interval (in years)\n"
				+ Minimum_Gestation.ToString() + ",Minimum_Gestation (in years)\n"
				+ Man_Too_Old_to_Marry.ToString() + ",Man_Too_Old_to_Marry but can continue to have kids after this old\n"
				+ "Per Year,Per Iteration,Description\n"
				+ First_Marriage_Probability_Per_Year.ToString() + "," + Marriage_Probability_Per_Iter[0].ToString() + ",First_Marriage_Probability (when a woman is available)\n"
				+ Second_Marriage_Probability_Per_Year.ToString() + "," + Marriage_Probability_Per_Iter[1].ToString() + ",Second_Marriage_Probability (when a woman is available)\n"
				+ Third_Plus_Marriage_Probability_Per_Year.ToString() + "," + Marriage_Probability_Per_Iter[2].ToString() + ",Third_Plus_Marriage_Probability (when a woman is available)\n"
				+ "Widows,Ladies,Girls,Boys,Men,Parameter / Diagnostic Description\n"
				+ ",," + Fema.Max_Age.ToString() + "," + Male.Max_Age.ToString() + ",,Max Age\n"
				+ Widows_PCBA.Array_Resizes.ToString() + "," + Available_Ladies.Array_Resizes.ToString() + ","
								+ Girls.Array_Resizes.ToString() + "," + Boys.Array_Resizes.ToString()
								+ "," + Households.Array_Resizes.ToString() + ",Array Resize Count\n"
				+ Widows_PCBA.strPeakCt() + Available_Ladies.strPeakCt() + Girls.strPeakCt()
								+ Boys.strPeakCt() + Households.strPeakCt() + "Peak Array Count\n"
				+ Widows_PCBA.Capacity() + Available_Ladies.Capacity() + Girls.Capacity()
								+ Boys.Capacity() + Households.Capacity() + "Array Capacity");

			Result_YTT.Append("\n" + "\nAverage Years Iterated" + table_header);
			Result_PkP.Append("\n" + "\nAverage Peak Iterated Population" + table_header);
			Result_TATGR.Append("\n" + "\nAverage Terminal Growth Rate for All Samples" + table_header);
			Result_NETGR.Append("\n" + "\nAverage Terminal Growth Rate for Non-Extinct Samples" + table_header);
			Result_Ext.Append("\n" + "\nSamples Resulting in Extinction " + table_header);
			for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
				Result_YTT.Append("\n" + Min_Gap_Between_Ch_Plan[xYBCB].ToString(DateFormat));
				Result_PkP.Append("\n" + Min_Gap_Between_Ch_Plan[xYBCB].ToString(DateFormat));
				Result_TATGR.Append("\n" + Min_Gap_Between_Ch_Plan[xYBCB].ToString(DateFormat));
				Result_NETGR.Append("\n" + Min_Gap_Between_Ch_Plan[xYBCB].ToString(DateFormat));
				Result_Ext.Append("\n" + Min_Gap_Between_Ch_Plan[xYBCB].ToString(DateFormat));
				for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
					Result_YTT.Append("," + Results[xMCBA, xYBCB].SamplAvg.Years_to_Max_Pop.ToString());
					Result_PkP.Append("," + Results[xMCBA, xYBCB].SamplAvg.Peak_Population.ToString());
					Result_TATGR.Append("," + Results[xMCBA, xYBCB].Tot_Avg_Term_Growth_Rate.ToString());
					Result_NETGR.Append("," + Results[xMCBA, xYBCB].SamplAvg.Terminal_Growth_Rate.ToString());
					Result_Ext.Append("," + Results[xMCBA, xYBCB].Extinction_Count.ToString());
					// now that we've reported the growth specifics (above), clear it for the next round.
					Results[xMCBA, xYBCB].Pop_At_Iter_Count_Clear();
				}
			}

			Result_YTT.Append(Result_PkP.ToString() + Result_TATGR.ToString() + Result_NETGR.ToString() + Result_Ext.ToString());

			for (xDate_at_Pop = 0; xDate_at_Pop < Report_Date_at_Pop.Length; xDate_at_Pop++) {
				Result_YTT.Append("\n" + "\nYears to Reach a Population of " + Report_Date_at_Pop[xDate_at_Pop].ToString());
				if (Report_Date_at_Pop[xDate_at_Pop] > Stop_Iterating_After_Pop) {
					Result_YTT.Append(" (All Values");
				} else {
					Result_YTT.Append(" (Values > " + Stop_Iterating_After_Year);
				}
				Result_YTT.Append(" are calculated from terminal growth rate - not captured from iteration alone)" + table_header);
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					Result_YTT.Append("\n" + Min_Gap_Between_Ch_Plan[xYBCB].ToString(DateFormat));
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Result_YTT.Append("," + Results[xMCBA, xYBCB].SamplAvg.Date_at_Pop[xDate_at_Pop].ToString(DateFormat));
					}
				}
			}

			for (xPop_at_Date = 0; xPop_at_Date < Report_Pop_at_Date.Length; xPop_at_Date++) {
				Result_YTT.Append("\n" + "\nPopulation after " + Report_Pop_at_Date[xPop_at_Date].ToString());
				if (Report_Pop_at_Date[xPop_at_Date] > Stop_Iterating_After_Year) {
					Result_YTT.Append(" Years (All Values");
				} else {
					Result_YTT.Append(" Years (Values > " + Stop_Iterating_After_Pop);
				}
				Result_YTT.Append(" are calculated from terminal growth rate - not captured from iteration alone)" + table_header);
				for (xYBCB = 0; xYBCB < Min_Gap_Between_Ch_Plan.Length; xYBCB++) {
					Result_YTT.Append("\n" + Min_Gap_Between_Ch_Plan[xYBCB].ToString(DateFormat));
					for (xMCBA = 0; xMCBA < Fema_Min_CBA_Plan.Length; xMCBA++) {
						Result_YTT.Append("," + Results[xMCBA, xYBCB].SamplAvg.Pop_at_Date[xPop_at_Date].ToString("0"));
					}
				}
			}

			Publish_Results_to_Files();
		}
		public static void Publish_Results_to_Files() {
			// we already have StringBuilder objects with the contents of the files. Dump them to disk here.
			File_Create_Dir(Results_Path);
			File_Write_Text(Results_Path + "Grand_Averages.csv", Result_YTT.ToString());
			File_Write_Text(Results_Path + "Permutation_Details.CSV", Result_SampDet.ToString());
		}
		public static bool File_Create_Dir(string PathName) {
			// creates a directory (folder). returns true on success or false on failure.
			try {
				Directory.CreateDirectory(PathName);
				return true; // success
			} catch {
				return false; // failure
			}
		}
		public static bool File_Write_Text(string FilePathName, string String_to_Write) {
			// writes a string to a file. returns true on success or false on failure.
			try {
				File.WriteAllText(FilePathName, String_to_Write);
				return true; // success
			} catch {
				return false; // failure
			}
		}
		public static bool File_Read_Text(string FilePathName, out string File_Contents) {
			// reads the entire contents of a text file into a string. Returns true if successful.
			// if unsuccessful, sets File_Contents to a blank string and returns false.
			try {
				File_Contents = File.ReadAllText(FilePathName);
				return true;
			} catch (Exception Err) {
				File_Contents = "";
				MessageBox.Show("Could not read the contents of \n" + FilePathName+ "\n" +Err.Message);
				return false;
			}
		}
		#endregion
	}
}
