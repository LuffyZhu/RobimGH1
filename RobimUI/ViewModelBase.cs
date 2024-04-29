using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Robim.RobimUI
{
    [Serializable]
    public class ViewModelBase : INotifyPropertyChanged
    {
        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyname = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }
    }
    public enum Resolution { VeryHight = 0, Hight = 1, Normal = 2, Low = 3, VeryLow = 4 }
    
    [Serializable]
    public class Resolution_Combobox : ViewModelBase
    {
        private Resolution resolution;

        public delegate void ResolutionChangeHandler();
        [field: NonSerializedAttribute()]
        public event ResolutionChangeHandler WhenResolutionChange;

        public Resolution_Combobox()
        {
            resolution = Resolution.VeryHight;
        }
        public int index
        {
            get { return (int)resolution; }
            set { resolution = (Resolution)value; if (WhenResolutionChange != null) WhenResolutionChange(); OnPropertyChanged(); }
        }
        public int[] GetColumnsCount(int total)
        {
            ///行数
            double total_columns = total;
            switch (resolution)///每行几笔
            {
                case Resolution.VeryHight:
                    break;
                case Resolution.Hight:
                    if (total / 2.0 < 1000)
                        total_columns = total;
                    else
                        total_columns /= 2.0;
                    break;
                case Resolution.Normal:
                    if (total / 10.0 < 100)
                        total_columns = total;
                    else
                        total_columns /= 10.0;
                    break;
                case Resolution.Low:
                    if (total < 100)
                        total_columns = total;
                    else
                        total_columns = 100;
                    break;
                case Resolution.VeryLow:
                    if (total < 45)
                        total_columns = total;
                    else
                        total_columns = 45;
                    break;
                default:
                    break;
            }
            int one_column = (int)(total / total_columns);
            List<int> ColumnCounts = new List<int>();
            for (int i = 0; i < (int)total_columns; i++)
            {
                ColumnCounts.Add(one_column);
            }
            int a = one_column * (int)total_columns;
            if (total - a != 0)
                ColumnCounts.Add(total - a);
            return ColumnCounts.ToArray();
        }
        public Resolution Resolution
        {
            get
            {
                return resolution;
            }
        }
    }
    public class Compute_Progress : ViewModelBase
    {
        private int progress;
        private Visibility visibility;
        public Compute_Progress()
        {
            visibility = Visibility.Hidden;
            progress = 0;
        }
        public Visibility Visibility
        {
            get { return visibility; }
            set { visibility = value; OnPropertyChanged(); }
        }
        public int Progress
        {
            get { return progress; }
            set { progress = value; OnPropertyChanged(); }
        }
    }

    [Serializable]
    public class Range_TextBox : ViewModelBase
    {
        private double maxvalue;
        private double minvalue;
        private double stepsize;
        private string maxstring;
        private string minstring;
        private string stepstring;

        public double[] degreesteps { get; private set; } = new double[3] { -180, 0, 180 };
        public int ZeroIndex { get; set; } = 0;

        public delegate void ValueChangeHandler(double aftervalue, bool ismaxvalue);
        [field: NonSerializedAttribute()]
        public event ValueChangeHandler WhenValueChange;
        public Range_TextBox()
        {
            maxstring = "180";
            minstring = "-180";
            stepstring = "90";
            maxvalue = 180;
            minvalue = -180;
            stepsize = 90;
        }
        public string MaxValue
        {
            get
            {
                return maxstring;
            }
            set
            {
                if (double.TryParse(value, out double result))
                {
                    double compare = result;
                    compare = Math.Min(compare, 360);//value vs 360 get min. //(不可大于360)
                    compare = Math.Max(compare, 0.1);//value vs 0.1 get max. //(不可小于等于0)
                    //compare = Math.Max(compare, minvalue + 0.1);//value vs -179.9(-180 + 0.1) get max. //(不可小于等于-180)
                    StepSize = Math.Min(stepsize, compare - minvalue).ToString("0.0");
                    if (WhenValueChange != null)
                    {
                        WhenValueChange(compare, true);
                    }
                    maxvalue = compare;
                    maxstring = maxvalue.ToString("0.0");
                }
                OnPropertyChanged();
            }
        }
        public string MinValue
        {
            get
            {
                return minstring;
            }
            set
            {
                if (double.TryParse(value, out double result))
                {
                    double compare = result;
                    //compare = Math.Min(compare, maxvalue - 0.1);//value vs 179.9(180 - 0.1) get max. //(不可大于等于180)
                    compare = Math.Min(compare, -0.1);//value vs -0.1 get max. //(不可大于等于0)
                    compare = Math.Max(compare, -360);//value vs -360 get max. //(不可小于-360)
                    StepSize = Math.Min(stepsize, maxvalue - compare).ToString("0.0");
                    if (WhenValueChange != null)
                    {
                        WhenValueChange(compare, false);
                    }
                    minvalue = compare;
                    minstring = minvalue.ToString("0.0");
                }
                OnPropertyChanged();
            }
        }
        public string StepSize
        {
            get
            {
                return stepstring;
            }
            set
            {
                if (double.TryParse(value, out double result))
                {
                    double compare = result;
                    compare = Math.Min(compare, maxvalue - minvalue);
                    compare = Math.Max(compare, 0.1);
                    stepsize = compare;
                    stepstring = stepsize.ToString("0.0");
                }
                OnPropertyChanged();
            }
        }
        public double[] DegreeSteps()
        {
            List<double> rotatedegrees = new List<double>();
            double count_min = Math.Abs(minvalue / stepsize);
            double count_max = maxvalue / stepsize;
            ///0 to -180
            for (int i = 0; i < count_min; i++)
            {
                double min = -stepsize * i;
                rotatedegrees.Add(min);
                if (i == count_min - 1 && min != minvalue)
                {
                    rotatedegrees.Add(minvalue);
                }
            }
            rotatedegrees.Reverse();
            ZeroIndex = rotatedegrees.Count - 1;
            ///first step to 180
            for (int i = 1; i < count_max; i++)
            {
                double max = stepsize * i;
                rotatedegrees.Add(max);
                if (i == count_max - 1 && max != maxvalue)
                {
                    rotatedegrees.Add(maxvalue);
                }
            }
            degreesteps = rotatedegrees.ToArray();
            return rotatedegrees.ToArray();
        }
    }
    /*
    [Serializable]
    public class FreeAxis_CheckBox : ViewModelBase
    {
        public TCP_Free_Axis_Mulit tcp_Free_Axis_Mulit { get; set; }
        public FreeAxis_CheckBox()
        {
            tcp_Free_Axis_Mulit = TCP_Free_Axis_Mulit.Z_Axis;
        }
        public bool X_Axis
        {
            get
            {
                if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.X_Axis) == TCP_Free_Axis_Mulit.X_Axis)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                {
                    if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.X_Axis) != TCP_Free_Axis_Mulit.X_Axis)
                        tcp_Free_Axis_Mulit |= TCP_Free_Axis_Mulit.X_Axis;
                }
                else
                {
                    if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.X_Axis) == TCP_Free_Axis_Mulit.X_Axis)
                        tcp_Free_Axis_Mulit ^= TCP_Free_Axis_Mulit.X_Axis;
                }
                OnPropertyChanged();
            }
        }
        public bool Y_Axis
        {
            get
            {
                if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.Y_Axis) == TCP_Free_Axis_Mulit.Y_Axis)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                {
                    if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.Y_Axis) != TCP_Free_Axis_Mulit.Y_Axis)
                        tcp_Free_Axis_Mulit |= TCP_Free_Axis_Mulit.Y_Axis;
                }
                else
                {
                    if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.Y_Axis) == TCP_Free_Axis_Mulit.Y_Axis)
                        tcp_Free_Axis_Mulit ^= TCP_Free_Axis_Mulit.Y_Axis;
                }
                OnPropertyChanged();
            }
        }
        public bool Z_Axis
        {
            get
            {
                if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.Z_Axis) == TCP_Free_Axis_Mulit.Z_Axis)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                {
                    if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.Z_Axis) != TCP_Free_Axis_Mulit.Z_Axis)
                        tcp_Free_Axis_Mulit |= TCP_Free_Axis_Mulit.Z_Axis;
                }
                else
                {
                    if ((tcp_Free_Axis_Mulit & TCP_Free_Axis_Mulit.Z_Axis) == TCP_Free_Axis_Mulit.Z_Axis)
                        tcp_Free_Axis_Mulit ^= TCP_Free_Axis_Mulit.Z_Axis;
                }
                OnPropertyChanged();
            }
        }
    }
    */
    #region enumselect
    [Serializable]
    public class EnumSelection<T> : ViewModelBase where T : struct, IComparable, IFormattable, IConvertible
    {
        private T value; // stored value of the Enum
        private bool isFlagged; // Enum uses flags?
        private bool canDeselect; // Can be deselected? (Radio buttons cannot deselect, checkboxes can)
        private T blankValue; // what is considered the "blank" value if it can be deselected?

        public EnumSelection(T value) : this(value, false, default(T)) { }
        public EnumSelection(T value, bool canDeselect) : this(value, canDeselect, default(T)) { }
        public EnumSelection(T value, T blankValue) : this(value, true, blankValue) { }
        public EnumSelection(T value, bool canDeselect, T blankValue)
        {
            if (!typeof(T).IsEnum) throw new ArgumentException($"{nameof(T)} must be an enum type"); // I really wish there was a way to constrain generic types to enums...
            isFlagged = typeof(T).IsDefined(typeof(FlagsAttribute), false);

            this.value = value;
            this.canDeselect = canDeselect;
            this.blankValue = blankValue;
        }

        public T Value
        {
            get { return value; }
            set
            {
                if (this.value.Equals(value)) return;
                this.value = value;
                OnPropertyChanged();
                OnPropertyChanged("Item[]"); // Notify that the indexer property has changed
            }
        }

        [IndexerName("Item")]
        public bool this[T key]
        {
            get
            {
                int iKey = (int)(object)key;
                return isFlagged ? ((int)(object)value & iKey) == iKey : value.Equals(key);
            }
            set
            {
                if (isFlagged)
                {
                    int iValue = (int)(object)this.value;
                    int iKey = (int)(object)key;

                    if (((iValue & iKey) == iKey) == value) return;

                    if (value)
                        Value = (T)(object)(iValue | iKey);
                    else
                        Value = (T)(object)(iValue & ~iKey);
                }
                else
                {
                    if (this.value.Equals(key) == value) return;
                    if (!value && !canDeselect) return;

                    Value = value ? key : blankValue;
                }
            }
        }
    }
    #endregion
}
