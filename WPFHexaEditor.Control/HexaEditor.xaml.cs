﻿//////////////////////////////////////////////
// Apache 2.0  - 2016-2017
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFHexaEditor.Core;
using WPFHexaEditor.Core.Bytes;
using WPFHexaEditor.Core.CharacterTable;
using WPFHexaEditor.Core.Interfaces;
using WPFHexaEditor.Core.MethodExtention;
using WPFHexaEditor.Control.Dialog;

namespace WPFHexaEditor.Control
{
    /// <summary> 
    /// WPF HexEditor control
    /// </summary>
    public partial class HexaEditor : UserControl, IDisposable
    {
        #region Global class variables
        //byte provider for work with file in stream currently loaded in control.
        private ByteProvider _provider = null;

        //The large change of scroll when clicked on bar
        private double _scrollLargeChange = 100;

        //List of byte are high light  (TODO: remplace by dictonnary
        private List<long> _markedPositionList = new List<long>();

        //Byte position in file of the right click;
        private long _rightClickBytePosition = -1;

        //Custom character table loaded.
        private TBLStream _TBLCharacterTable = null;

        //Hold the count of all byte in file. 
        private long[] _bytecount;

        //Save the buffer as a field,To save the time when Scolling.not building them every time when scolling;
        private byte[] _viewBuffer;
        private long _priLevel = 0;

        //Used with VerticalMoveByTime methods/events to move the scrollbar
        private bool _mouseOnBottom = false;
        private long _bottomEnterTimes = 0;
        private bool _mouseOnTop = false;
        private long _topEnterTimes = 0;
        #endregion Global Class variables

        #region Events
        /// <summary>
        /// Occurs when selection start are changed.
        /// </summary>
        public event EventHandler SelectionStartChanged;

        /// <summary>
        /// Occurs when selection stop are changed.
        /// </summary>
        public event EventHandler SelectionStopChanged;

        /// <summary>
        /// Occurs when the lenght of selection are changed.
        /// </summary>
        public event EventHandler SelectionLenghtChanged;

        /// <summary>
        /// Occurs when data are copie to clipboard.
        /// </summary>
        public event EventHandler DataCopied;

        /// <summary>
        /// Occurs when the type of character table are changed.
        /// </summary>
        public event EventHandler TypeOfCharacterTableChanged;

        /// <summary>
        /// Occurs when a long process percent changed.
        /// </summary>
        public event EventHandler LongProcessProgressChanged;

        /// <summary>
        /// Occurs when a long process are started.
        /// </summary>
        public event EventHandler LongProcessProgressStarted;

        /// <summary>
        /// Occurs when a long process are completed.
        /// </summary>
        public event EventHandler LongProcessProgressCompleted;

        /// <summary>
        /// Occurs when readonly property are changed.
        /// </summary>
        public event EventHandler ReadOnlyChanged;

        /// <summary>
        /// Occurs when data are saved to stream/file.
        /// </summary>
        public event EventHandler ChangesSubmited;

        /// <summary>
        /// Occurs when the replace byte by byte are completed
        /// </summary>
        public event EventHandler ReplaceByteCompleted;

        /// <summary>
        /// Occura when the fill with byte method are completed
        /// </summary>
        public event EventHandler FillWithByteCompleted;

        #endregion Events

        #region Constructor
        public HexaEditor()
        {
            InitializeComponent();

            //Load default build-in TBL
            //TypeOfCharacterTable = CharacterTableType.TBLFile;
            //LoadDefaultTBL(DefaultCharacterTableType.ASCII);

            //Refresh view
            UpdateScrollBar();
            RefreshView(true);

            DataContext = this;
        }
        #endregion Contructor

        #region Build-in CTRL key property
        public bool AllowBuildinCTRLC
        {
            get => (bool)GetValue(AllowBuildinCTRLCProperty);
            set => SetValue(AllowBuildinCTRLCProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLC.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCTRLCProperty =
            DependencyProperty.Register(nameof(AllowBuildinCTRLC), typeof(bool), typeof(HexaEditor),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(Control_AllowBuildinCTRLPropertyChanged)));

        public bool AllowBuildinCTRLV
        {
            get => (bool)GetValue(AllowBuildinCTRLVProperty);
            set => SetValue(AllowBuildinCTRLVProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLV.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCTRLVProperty =
            DependencyProperty.Register(nameof(AllowBuildinCTRLV), typeof(bool), typeof(HexaEditor),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(Control_AllowBuildinCTRLPropertyChanged)));

        public bool AllowBuildinCTRLA
        {
            get => (bool)GetValue(AllowBuildinCTRLAProperty);
            set => SetValue(AllowBuildinCTRLAProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLA.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCTRLAProperty =
            DependencyProperty.Register(nameof(AllowBuildinCTRLA), typeof(bool), typeof(HexaEditor),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(Control_AllowBuildinCTRLPropertyChanged)));

        public bool AllowBuildinCTRLZ
        {
            get => (bool)GetValue(AllowBuildinCTRLZProperty);
            set => SetValue(AllowBuildinCTRLZProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCTRLZProperty =
            DependencyProperty.Register(nameof(AllowBuildinCTRLZ), typeof(bool), typeof(HexaEditor),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(Control_AllowBuildinCTRLPropertyChanged)));

        private static void Control_AllowBuildinCTRLPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                    ctrl.RefreshView();
        }
        #endregion Build-in CTRL key property

        #region Colors/fonts property and methods
        public Brush SelectionFirstColor
        {
            get => (Brush)GetValue(SelectionFirstColorProperty);
            set => SetValue(SelectionFirstColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectionFirstColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionFirstColorProperty =
            DependencyProperty.Register(nameof(SelectionFirstColor), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.RoyalBlue, new PropertyChangedCallback(Control_ColorPropertyChanged)));

        public Brush SelectionSecondColor
        {
            get => (Brush)GetValue(SelectionSecondColorProperty);
            set => SetValue(SelectionSecondColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectionFirstColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionSecondColorProperty =
            DependencyProperty.Register(nameof(SelectionSecondColor), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.LightSteelBlue, new PropertyChangedCallback(Control_ColorPropertyChanged)));

        public Brush ByteModifiedColor
        {
            get => (Brush)GetValue(ByteModifiedColorProperty);
            set => SetValue(ByteModifiedColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteModifiedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteModifiedColorProperty =
            DependencyProperty.Register(nameof(ByteModifiedColor), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.DarkGray, new PropertyChangedCallback(Control_ColorPropertyChanged)));

        public Brush MouseOverColor
        {
            get => (Brush)GetValue(MouseOverColorProperty);
            set => SetValue(MouseOverColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for MouseOverColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MouseOverColorProperty =
            DependencyProperty.Register(nameof(MouseOverColor), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.LightSkyBlue, new PropertyChangedCallback(Control_ColorPropertyChanged)));

        public Brush ByteDeletedColor
        {
            get => (Brush)GetValue(ByteDeletedColorProperty);
            set => SetValue(ByteDeletedColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteDeletedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteDeletedColorProperty =
            DependencyProperty.Register(nameof(ByteDeletedColor), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.Red, new PropertyChangedCallback(Control_ColorPropertyChanged)));

        public Brush HighLightColor
        {
            get => (Brush)GetValue(HighLightColorProperty);
            set => SetValue(HighLightColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for HighLightColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighLightColorProperty =
            DependencyProperty.Register(nameof(HighLightColor), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.Gold, new PropertyChangedCallback(Control_ColorPropertyChanged)));

        public Brush ForegroundOffSetHeaderColor
        {
            get => (Brush)GetValue(ForegroundOffSetHeaderColorProperty);
            set => SetValue(ForegroundOffSetHeaderColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundOffSetHeaderColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundOffSetHeaderColorProperty =
            DependencyProperty.Register(nameof(ForegroundOffSetHeaderColor), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.Gray, new PropertyChangedCallback(Control_ForegroundOffSetHeaderColorPropertyChanged)));

        private static void Control_ForegroundOffSetHeaderColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                {
                    ctrl.UpdateHeader();
                    ctrl.UpdateLinesOffSet();
                }
        }

        public new Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        // Using a DependencyProperty as the backing store for Foreground.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.Black, new PropertyChangedCallback(Control_ColorPropertyChanged)));

        public Brush ForegroundContrast
        {
            get => (Brush)GetValue(ForegroundContrastProperty);
            set => SetValue(ForegroundContrastProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundContrastColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundContrastProperty =
            DependencyProperty.Register(nameof(ForegroundContrast), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.White, new PropertyChangedCallback(Control_ColorPropertyChanged)));

        // Using a DependencyProperty as the backing store for  Background.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.White, new PropertyChangedCallback(Control_BackgroundColorPropertyChanged)));

        private static void Control_BackgroundColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                    ctrl.BaseGrid.Background = (Brush)e.NewValue;
        }

        private static void Control_ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                    ctrl.UpdateVisual();
        }

        public new FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        // Using a DependencyProperty as the backing store for FontFamily.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(HexaEditor),
                new FrameworkPropertyMetadata(new FontFamily("Courier New"),
                    new PropertyChangedCallback(FontFamily_Changed)));

        private static void FontFamily_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                    ctrl.RefreshView(true);
        }

        /// <summary>
        /// Call Updatevisual methods for all IByteControl
        /// </summary>
        public void UpdateVisual() => TraverseHexAndStringBytes(ctrl => { ctrl.UpdateVisual(); });

        #endregion Colors/fonts property and methods

        #region Miscellaneous property/methods

        public double ScrollLargeChange
        {
            get => _scrollLargeChange;
            set
            {
                _scrollLargeChange = value;
                UpdateScrollBar();
            }
        }

        /// <summary>
        /// Height of data line. 
        /// </summary>
        public double LineHeight
        {
            get => (double)GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for LineHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(HexaEditor),
                new FrameworkPropertyMetadata(18D,
                    new PropertyChangedCallback(LineHeight_PropertyChanged)));

        private static void LineHeight_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                ctrl.RefreshView();
        }

        /// <summary>
        /// Control the mouse wheel speed
        /// </summary>
        public MouseWheelSpeed MouseWheelSpeed { get; set; } = MouseWheelSpeed.Normal;

        #endregion Miscellaneous property/methods

        #region Characters tables property/methods

        /// <summary>
        /// Type of caracter table are used un hexacontrol.
        /// For now, somes character table can be readonly but will change in future
        /// </summary>
        public CharacterTableType TypeOfCharacterTable
        {
            get => (CharacterTableType)GetValue(TypeOfCharacterTableProperty);
            set => SetValue(TypeOfCharacterTableProperty, value);
        }

        // Using a DependencyProperty as the backing store for TypeOfCharacterTable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TypeOfCharacterTableProperty =
            DependencyProperty.Register(nameof(TypeOfCharacterTable), typeof(CharacterTableType), typeof(HexaEditor),
                new FrameworkPropertyMetadata(CharacterTableType.ASCII,
                    new PropertyChangedCallback(TypeOfCharacterTable_PropertyChanged)));

        private static void TypeOfCharacterTable_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
            {
                ctrl.RefreshView(true);
                ctrl.TypeOfCharacterTableChanged?.Invoke(ctrl, new EventArgs());
            }
        }

        /// <summary>
        /// Show or not Multi Title Enconding (MTE) are loaded in TBL file
        /// </summary>
        public bool TBLShowMTE
        {
            get => (bool)GetValue(TBLShowMTEProperty);
            set => SetValue(TBLShowMTEProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLShowMTE.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TBLShowMTEProperty =
            DependencyProperty.Register(nameof(TBLShowMTE), typeof(bool), typeof(HexaEditor),
                new FrameworkPropertyMetadata(true,
                    new PropertyChangedCallback(TBLShowMTE_PropetyChanged)));

        private static void TBLShowMTE_PropetyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                ctrl.RefreshView();
        }

        /// <summary>
        /// Load TBL Character table file in control. (Used for ROM reverse engineering)
        /// Load TBL Bookmark into control.
        /// Change CharacterTable property for use.
        /// </summary>
        public void LoadTBLFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                _TBLCharacterTable = new TBLStream(fileName);

                TBLLabel.Visibility = Visibility.Visible;
                TBLLabel.ToolTip = $"TBL file : {fileName}";

                UpdateTBLBookMark();

                BuildDataLines((int)MaxVisibleLine, true);
                RefreshView(true, true);
            }
        }

        /// <summary>
        /// Load TBL Character table file in control. (Used for ROM reverse engineering)
        /// Load TBL Bookmark into control.
        /// Change CharacterTable property for use.
        /// </summary>
        public void LoadDefaultTBL(DefaultCharacterTableType type = DefaultCharacterTableType.ASCII)
        {
            _TBLCharacterTable = TBLStream.CreateDefaultASCII();
            TBLShowMTE = false;

            TBLLabel.Visibility = Visibility.Visible;
            TBLLabel.ToolTip = $"Default TBL : {type}";

            RefreshView();
        }

        /// <summary>
        /// Update TBL bookmark in control
        /// </summary>
        private void UpdateTBLBookMark()
        {
            //Load from loaded TBL bookmark
            if (_TBLCharacterTable != null)
                foreach (BookMark mark in _TBLCharacterTable.BookMarks)
                    SetScrollMarker(mark);
        }

        /// <summary>
        /// Get or set the color of DTE in string panel.
        /// </summary>
        public SolidColorBrush TBLDTEColor
        {
            get => (SolidColorBrush)GetValue(TBLDTEColorProperty);
            set => SetValue(TBLDTEColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TBLDTEColorProperty =
            DependencyProperty.Register(nameof(TBLDTEColor), typeof(SolidColorBrush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.Red,
                    new PropertyChangedCallback(TBLColor_Changed)));

        private static void TBLColor_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                ctrl.RefreshView();
        }

        /// <summary>
        /// Get or set the color of MTE in string panel.
        /// </summary>
        public SolidColorBrush TBLMTEColor
        {
            get => (SolidColorBrush)GetValue(TBLMTEColorProperty);
            set => SetValue(TBLMTEColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TBLMTEColorProperty =
            DependencyProperty.Register(nameof(TBLMTEColor), typeof(SolidColorBrush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.DarkBlue,
                    new PropertyChangedCallback(TBLColor_Changed)));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TBLEndBlockColor
        {
            get => (SolidColorBrush)GetValue(TBLEndBlockColorProperty);
            set => SetValue(TBLEndBlockColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TBLEndBlockColorProperty =
            DependencyProperty.Register(nameof(TBLEndBlockColor), typeof(SolidColorBrush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.Blue,
                    new PropertyChangedCallback(TBLColor_Changed)));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TBLEndLineColor
        {
            get => (SolidColorBrush)GetValue(TBLEndLineColorProperty);
            set => SetValue(TBLEndLineColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TBLEndLineColorProperty =
            DependencyProperty.Register(nameof(TBLEndLineColor), typeof(SolidColorBrush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.Blue,
                    new PropertyChangedCallback(TBLColor_Changed)));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TBLDefaultColor
        {
            get => (SolidColorBrush)GetValue(TBLDefaultColorProperty);
            set => SetValue(TBLDefaultColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TBLDefaultColorProperty =
            DependencyProperty.Register(nameof(TBLDefaultColor), typeof(SolidColorBrush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.Black,
                    new PropertyChangedCallback(TBLColor_Changed)));

        #endregion Characters tables property/methods

        #region ReadOnly property/event

        /// <summary>
        /// Put the control on readonly mode.
        /// </summary>
        public bool ReadOnlyMode
        {
            get => (bool)GetValue(ReadOnlyModeProperty);
            set => SetValue(ReadOnlyModeProperty, value);
        }

        public static readonly DependencyProperty ReadOnlyModeProperty =
            DependencyProperty.Register("ReadOnlyMode", typeof(bool), typeof(HexaEditor),
                new FrameworkPropertyMetadata(false,
                    new PropertyChangedCallback(ReadOnlyMode_PropertyChanged)));

        private static void ReadOnlyMode_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                    ctrl.RefreshView(true);
        }

        private void Provider_ReadOnlyChanged(object sender, EventArgs e)
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                ReadOnlyMode = _provider.ReadOnlyMode;
                ReadOnlyChanged?.Invoke(this, new EventArgs());
            }
        }

        #endregion ReadOnly property/event

        #region Add modify delete bytes methods/event

        private void Control_ByteModified(object sender, EventArgs e)
        {
            if (sender is IByteControl ctrl)
            {
                _provider.AddByteModified(ctrl.Byte, ctrl.BytePositionInFile);
                SetScrollMarker(ctrl.BytePositionInFile, ScrollMarker.ByteModified);
                UpdateByteModified();
            }

            UpdateStatusBar();
        }

        /// <summary>
        /// Delete selection, add scroll marker and update control
        /// </summary>
        public void DeleteSelection()
        {
            if (!CanDelete()) return;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                long position = -1;

                if (SelectionStart > SelectionStop)
                    position = SelectionStop;
                else
                    position = SelectionStart;

                _provider.AddByteDeleted(position, SelectionLength);

                SetScrollMarker(position, ScrollMarker.ByteDeleted);

                UpdateByteModified();
                UpdateSelection();
                UpdateStatusBar();
            }
        }
        
        /// <summary>
        /// Obtain the max line for verticalscrollbar
        /// </summary>
        public long MaxLine
        {
            get
            {
                if (ByteProvider.CheckIsOpen(_provider))
                    return _provider.Length / BytePerLine;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Get the number of row visible in control
        /// </summary>
        public long MaxVisibleLine
        {
            get
            {
                var actualheight = (ActualHeight - HexHeaderStackPanel.ActualHeight - StatusBarGrid.ActualHeight);

                if (actualheight < 0) actualheight = 0;

                return (long)(actualheight / LineHeight) + 1;
            }
        }

        #endregion Lines methods

        #region Selection Property/Methods/Event

        /// <summary>
        /// Get the selected line of focus control
        /// </summary>
        public long SelectionLine
        {
            get => (long)GetValue(SelectionLineProperty);
            internal set => SetValue(SelectionLineProperty, value);
        }

        public static readonly DependencyProperty SelectionLineProperty =
            DependencyProperty.Register("SelectionLine", typeof(long), typeof(HexaEditor),
                new FrameworkPropertyMetadata(0L));

        private void LineInfoLabel_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock line)
                if (e.LeftButton == MouseButtonState.Pressed)
                    SelectionStop = ByteConverters.HexLiteralToLong(line.Text).position + BytePerLine - 1;
        }

        private void LineInfoLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock line)
            {
                SelectionStart = ByteConverters.HexLiteralToLong(line.Text).position;
                SelectionStop = SelectionStart + BytePerLine - 1;
            }
        }

        private void Control_ByteDeleted(object sender, EventArgs e) => DeleteSelection();

        private void Control_EscapeKey(object sender, EventArgs e)
        {
            UnSelectAll();
            UnHighLightAll();
            Focus();
        }

        private void Control_CTRLZKey(object sender, EventArgs e) => Undo();

        private void Control_CTRLCKey(object sender, EventArgs e) => CopyToClipboard();

        private void Control_CTRLAKey(object sender, EventArgs e) => SelectAll();

        private void Control_CTRLVKey(object sender, EventArgs e) => PasteWithoutInsert();

        private void Control_MovePageUp(object sender, EventArgs e)
        {
            long visibleLine = MaxVisibleLine;
            long byteToMove = (BytePerLine * visibleLine);
            long test = SelectionStart - byteToMove;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test > -1)
                    SelectionStart -= byteToMove;
                else
                    SelectionStart = 0;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test > -1)
                {
                    SelectionStart -= byteToMove;
                    SelectionStop -= byteToMove;
                }
            }

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            if (sender is HexByte || sender is StringByte)
            {
                VerticalScrollBar.Value -= visibleLine - 1;
                SetFocusAtSelectionStart(sender as IByteControl);
            }
        }

        private void Control_MovePageDown(object sender, EventArgs e)
        {
            long visibleLine = MaxVisibleLine;
            long byteToMove = (BytePerLine * visibleLine);
            long test = SelectionStart + byteToMove;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test < _provider.Length)
                    SelectionStart += byteToMove;
                else
                    SelectionStart = _provider.Length;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test < _provider.Length)
                {
                    SelectionStart += byteToMove;
                    SelectionStop += byteToMove;
                }
            }

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            if (sender is HexByte || sender is StringByte)
            {
                VerticalScrollBar.Value += visibleLine - 1;
                SetFocusAtSelectionStart(sender as IByteControl);
            }
        }

        private void Control_MoveDown(object sender, EventArgs e)
        {
            long test = SelectionStart + BytePerLine;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test < _provider.Length)
                    SelectionStart += BytePerLine;
                else
                    SelectionStart = _provider.Length;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test < _provider.Length)
                {
                    SelectionStart += BytePerLine;
                    SelectionStop += BytePerLine;
                }
            }

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            SetFocusAtSelectionStart(sender as IByteControl);
        }

        private void Control_MoveUp(object sender, EventArgs e)
        {
            long test = SelectionStart - BytePerLine;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test > -1)
                    SelectionStart -= BytePerLine;
                else
                    SelectionStart = 0;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test > -1)
                {
                    SelectionStart -= BytePerLine;
                    SelectionStop -= BytePerLine;
                }
            }

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            SetFocusAtSelectionStart(sender as IByteControl);
        }

        private void Control_Click(object sender, EventArgs e)
        {
            if (sender is IByteControl ctrl)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    SelectionStop = ctrl.BytePositionInFile;
                else
                {
                    SelectionStart = ctrl.BytePositionInFile;
                    SelectionStop = ctrl.BytePositionInFile;
                }

                if (ctrl is StringByte)
                    UpdateSelectionColor(FirstColor.StringByteData);
                else
                    UpdateSelectionColor(FirstColor.HexByteData);

                UpdateVisual();
            }
        }

        private void Control_MouseSelection(object sender, EventArgs e)
        {
            //Prevent false mouse selection on file open
            if (SelectionStart == -1)
                return;

            if (sender is IByteControl bCtrl)
            {
                IInputElement focusedControl = Keyboard.FocusedElement;

                //update selection
                if (bCtrl.BytePositionInFile != -1)
                    SelectionStop = bCtrl.BytePositionInFile;
                else
                    SelectionStop = LastVisibleBytePosition;

                if (focusedControl is HexByte)
                    UpdateSelectionColor(FirstColor.HexByteData);
                else
                    UpdateSelectionColor(FirstColor.StringByteData);

                UpdateSelection();
            }
        }

        /// <summary>
        /// Un highlight all byte as highlighted with find all methods
        /// </summary>
        public void UnHighLightAll()
        {
            _markedPositionList.Clear();
            UpdateHighLight();
            ClearScrollMarker(ScrollMarker.SearchHighLight);
        }

        /// <summary>
        /// Set the start byte position of selection
        /// </summary>
        public long SelectionStart
        {
            get => (long)GetValue(SelectionStartProperty);
            set => SetValue(SelectionStartProperty, value);
        }

        public static readonly DependencyProperty SelectionStartProperty =
            DependencyProperty.Register("SelectionStart", typeof(long), typeof(HexaEditor),
                new FrameworkPropertyMetadata(-1L, new PropertyChangedCallback(SelectionStart_ChangedCallBack),
                    new CoerceValueCallback(SelectionStart_CoerceValueCallBack)));

        private static object SelectionStart_CoerceValueCallBack(DependencyObject d, object baseValue)
        {
            if (d is HexaEditor ctrl)
            {
                long value = (long)baseValue;

                if (value < -1)
                    return -1L;

                if (!ByteProvider.CheckIsOpen(ctrl._provider))
                    return -1L;
                else
                    return baseValue;
            }
            else return -1L;
        }

        private static void SelectionStart_ChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                {
                    if (ByteProvider.CheckIsOpen(ctrl._provider))
                        ctrl.SelectionByte = ctrl._provider.GetByte(ctrl.SelectionStart);
                    else
                        ctrl.SelectionByte = null;

                    ctrl.UpdateSelection();
                    ctrl.UpdateSelectionLine();
                    ctrl.UpdateVisual();
                    ctrl.UpdateStatusBar();
                    ctrl.SetScrollMarker(0, ScrollMarker.SelectionStart);

                    ctrl.SelectionStartChanged?.Invoke(ctrl, new EventArgs());
                    ctrl.SelectionLenghtChanged?.Invoke(ctrl, new EventArgs());
                }
        }

        /// <summary>
        /// Set the start byte position of selection
        /// </summary>
        public long SelectionStop
        {
            get => (long)GetValue(SelectionStopProperty);
            set => SetValue(SelectionStopProperty, value);
        }

        public static readonly DependencyProperty SelectionStopProperty =
            DependencyProperty.Register("SelectionStop", typeof(long), typeof(HexaEditor),
                new FrameworkPropertyMetadata(-1L, new PropertyChangedCallback(SelectionStop_ChangedCallBack),
                    new CoerceValueCallback(SelectionStop_CoerceValueCallBack)));

        private static object SelectionStop_CoerceValueCallBack(DependencyObject d, object baseValue)
        {
            if (d is HexaEditor ctrl)
            {
                long value = (long)baseValue;

                if (value < -1)
                    return -1L;

                if (!ByteProvider.CheckIsOpen(ctrl._provider))
                    return -1L;

                if (value >= ctrl._provider.Length)
                    return ctrl._provider.Length;
            }

            return baseValue;
        }

        private static void SelectionStop_ChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                {
                    ctrl.UpdateSelection();
                    ctrl.UpdateSelectionLine();

                    ctrl.SelectionStopChanged?.Invoke(ctrl, new EventArgs());
                    ctrl.SelectionLenghtChanged?.Invoke(ctrl, new EventArgs());
                }
        }

        /// <summary>
        /// Reset selection to -1
        /// </summary>
        public void UnSelectAll()
        {
            SelectionStart = -1;
            SelectionStop = -1;
        }

        /// <summary>
        /// Select the entire file
        /// If file are closed the selection will be set to -1
        /// </summary>
        public void SelectAll()
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                SelectionStart = 0;
                SelectionStop = _provider.Length;
            }
            else
            {
                SelectionStart = -1;
                SelectionStop = -1;
            }

            UpdateSelection();
        }

        /// <summary>
        /// Get the lenght of byte are selected (base 1)
        /// </summary>
        public long SelectionLength
        {
            get
            {
                if (SelectionStop == -1 || SelectionStop == -1)
                    return 0;
                else if (SelectionStart == SelectionStop)
                    return 1;
                else if (SelectionStart > SelectionStop)
                    return SelectionStart - SelectionStop + 1;
                else
                    return SelectionStop - SelectionStart + 1;
            }
        }

        /// <summary>
        /// Get byte array from current selection
        /// </summary>
        public byte[] SelectionByteArray
        {
            get
            {
                MemoryStream ms = new MemoryStream();
                CopyToStream(ms, true);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Get string from current selection
        /// </summary>
        public string SelectionString
        {
            get
            {
                MemoryStream ms = new MemoryStream();
                CopyToStream(ms, true);
                return ByteConverters.BytesToString(ms.ToArray());
            }
        }

        /// <summary>
        /// Get Hexadecimal from current selection
        /// </summary>
        public string SelectionHexa
        {
            get
            {
                MemoryStream ms = new MemoryStream();
                CopyToStream(ms, true);
                return ByteConverters.ByteToHex(ms.ToArray());
            }
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) //UP
                VerticalScrollBar.Value -= e.Delta / 120 * (int)MouseWheelSpeed;

            if (e.Delta < 0) //Down
                VerticalScrollBar.Value += e.Delta / 120 * -(int)MouseWheelSpeed;
        }

        private void Control_MoveRight(object sender, EventArgs e)
        {
            long test = SelectionStart + 1;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test <= _provider.Length)
                    SelectionStart++;
                else
                    SelectionStart = _provider.Length;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test < _provider.Length)
                {
                    SelectionStart++;
                    SelectionStop++;
                }
            }

            if (SelectionStart >= _provider.Length)
                SelectionStart = _provider.Length;

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            SetFocusAtSelectionStart(sender as IByteControl);
        }

        private void Control_MoveLeft(object sender, EventArgs e)
        {
            long test = SelectionStart - 1;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test > -1)
                    SelectionStart--;
                else
                    SelectionStart = 0;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test > -1)
                {
                    SelectionStart--;
                    SelectionStop--;
                }
            }

            if (SelectionStart < 0)
                SelectionStart = 0;

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            SetFocusAtSelectionStart(sender as IByteControl);
        }

        private void SetFocusAtSelectionStart(IByteControl sender)
        {
            switch (sender)
            {
                case HexByte hb:
                    SetFocusHexDataPanel(SelectionStart);
                    break;
                case StringByte sb:
                    SetFocusStringDataPanel(SelectionStart);
                    break;
            }
        }

        private void Control_MovePrevious(object sender, EventArgs e)
        {
            UpdateByteModified();

            //Call move left event
            Control_MoveLeft(sender, new EventArgs());
        }

        private void Control_MoveNext(object sender, EventArgs e)
        {
            UpdateByteModified();

            //Call moveright event
            Control_MoveRight(sender, new EventArgs());
        }
        #endregion Selection Property/Methods/Event

        #region Copy/Paste/Cut Methods

        /// <summary>
        /// Paste clipboard string without inserting byte at selection start
        /// </summary>
        private void PasteWithoutInsert()
        {
            if (ByteProvider.CheckIsOpen(_provider))
                if (SelectionStart > -1)
                {
                    _provider.PasteNotInsert(SelectionStart, Clipboard.GetText());
                    SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, "Paste from clipboard");
                    RefreshView();
                }
        }

        /// <summary>
        /// Fill the selection with a Byte at selection start
        /// </summary>
        public void FillWithByte(byte val) => FillWithByte(SelectionStart, SelectionLength, val);

        /// <summary>
        /// Fill with a Byte at start position
        /// </summary>
        public void FillWithByte(long startPosition, long length, byte val)
        {
            if (ByteProvider.CheckIsOpen(_provider))
                if (startPosition > -1 && length > 0)
                {
                    _provider.FillWithByte(startPosition, length, val);
                    SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, "Fill selection with byte");
                    RefreshView();
                }
        }

        /// <summary>
        /// Replace byte with another at selection position
        /// </summary>
        public void ReplaceByte(byte original, byte replace) => ReplaceByte(SelectionStart, SelectionLength, original, replace);

        /// <summary>
        /// Replace byte with another at start position
        /// </summary>
        public void ReplaceByte(long startPosition, long length, byte original, byte replace)
        {
            if (ByteProvider.CheckIsOpen(_provider))
                if (startPosition > -1 && length > 0)
                {
                    _provider.ReplaceByte(startPosition, length, original, replace);
                    SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, "replace with byte");
                    RefreshView();
                }
        }

        /// <summary>
        /// Return true if Copy method could be invoked.
        /// </summary>
        public bool CanCopy()
        {
            if (SelectionLength < 1 || !ByteProvider.CheckIsOpen(_provider))
                return false;

            return true;
        }

        /// <summary>
        /// Return true if delete method could be invoked.
        /// </summary>
        public bool CanDelete() => CanCopy() && !ReadOnlyMode;

        /// <summary>
        /// Copy to clipboard with default CopyPasteMode.ASCIIString
        /// </summary>
        public void CopyToClipboard() => CopyToClipboard(CopyPasteMode.ASCIIString);

        /// <summary>
        /// Copy to clipboard the current selection with actual change in control
        /// </summary>
        public void CopyToClipboard(CopyPasteMode copypastemode) => CopyToClipboard(copypastemode, SelectionStart, SelectionStop, true, _TBLCharacterTable);

        /// <summary>
        /// Copy to clipboard
        /// </summary>
        public void CopyToClipboard(CopyPasteMode copypastemode, long selectionStart, long selectionStop, bool copyChange, TBLStream tbl)
        {
            if (!CanCopy()) return;

            if (ByteProvider.CheckIsOpen(_provider))
                _provider.CopyToClipboard(copypastemode, selectionStart, selectionStop, copyChange, tbl);
        }

        /// <summary>
        /// Copy selection to a stream
        /// </summary>
        /// <param name="output">Output stream is not closed after copy</param>
        public void CopyToStream(Stream output, bool copyChange) => CopyToStream(output, SelectionStart, SelectionStop, copyChange);

        /// <summary>
        /// Copy selection to a stream
        /// </summary>
        /// <param name="output">Output stream is not closed after copy</param>
        public void CopyToStream(Stream output, long selectionStart, long selectionStop, bool copyChange)
        {
            if (!CanCopy()) return;

            if (ByteProvider.CheckIsOpen(_provider))
                _provider.CopyToStream(output, selectionStart, selectionStop, copyChange);
        }

        /// <summary>
        /// Occurs when data is copied in byteprovider instance
        /// </summary>
        private void Provider_DataCopied(object sender, EventArgs e) => DataCopied?.Invoke(sender, e);

        #endregion Copy/Paste/Cut Methods

        #region Set position methods

        /// <summary>
        /// Set position of cursor
        /// </summary>
        public void SetPosition(long position, long byteLenght)
        {
            SelectionStart = position;
            SelectionStop = position + byteLenght - 1;

            if (ByteProvider.CheckIsOpen(_provider))
                VerticalScrollBar.Value = GetLineNumber(position);
            else
                VerticalScrollBar.Value = 0;
        }

        /// <summary>
        /// Get the line number of position in parameter
        /// </summary>
        public double GetLineNumber(long position) => position / BytePerLine;

        /// <summary>
        /// Set position in control at position in parameter
        /// </summary>
        public void SetPosition(long position) => SetPosition(position, 0);

        /// <summary>
        /// Set position in control at position in parameter
        /// </summary>
        public void SetPosition(string HexLiteralPosition) => SetPosition(ByteConverters.HexLiteralToLong(HexLiteralPosition).position);

        /// <summary>
        /// Set position in control at position in parameter with specified selected lenght
        /// </summary>
        public void SetPosition(string HexLiteralPosition, long byteLenght) => SetPosition(ByteConverters.HexLiteralToLong(HexLiteralPosition).position, byteLenght);

        #endregion Set position methods

        #region Visibility property

        /// <summary>
        /// Set or Get value for change visibility of hexadecimal panel
        /// </summary>
        public Visibility HexDataVisibility
        {
            get => (Visibility)GetValue(HexDataVisibilityProperty);
            set => SetValue(HexDataVisibilityProperty, value);
        }

        public static readonly DependencyProperty HexDataVisibilityProperty =
            DependencyProperty.Register("HexDataVisibility", typeof(Visibility), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    new PropertyChangedCallback(HexDataVisibility_PropertyChanged),
                    new CoerceValueCallback(Visibility_CoerceValue)));

        private static object Visibility_CoerceValue(DependencyObject d, object baseValue)
        {
            Visibility value = (Visibility)baseValue;

            if (value == Visibility.Hidden)
                return Visibility.Collapsed;

            return value;
        }

        private static void HexDataVisibility_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
            {
                Visibility value = (Visibility)e.NewValue;

                switch (value)
                {
                    case Visibility.Visible:
                        ctrl.HexDataStackPanel.Visibility = Visibility.Visible;

                        if (ctrl.HeaderVisibility == Visibility.Visible)
                            ctrl.HexHeaderStackPanel.Visibility = Visibility.Visible;
                        break;

                    case Visibility.Collapsed:
                        ctrl.HexDataStackPanel.Visibility = Visibility.Collapsed;
                        ctrl.HexHeaderStackPanel.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        /// <summary>
        /// Set or Get value for change visibility of hexadecimal header
        /// </summary>
        public Visibility HeaderVisibility
        {
            get => (Visibility)GetValue(HeaderVisibilityProperty);
            set => SetValue(HeaderVisibilityProperty, value);
        }

        public static readonly DependencyProperty HeaderVisibilityProperty =
            DependencyProperty.Register("HeaderVisibility", typeof(Visibility), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    new PropertyChangedCallback(HeaderVisibility_PropertyChanged),
                    new CoerceValueCallback(Visibility_CoerceValue)));

        private static void HeaderVisibility_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
            {
                Visibility value = (Visibility)e.NewValue;

                switch (value)
                {
                    case Visibility.Visible:
                        if (ctrl.HexDataVisibility == Visibility.Visible)
                            ctrl.HexHeaderStackPanel.Visibility = Visibility.Visible;
                        break;

                    case Visibility.Collapsed:
                        ctrl.HexHeaderStackPanel.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        /// <summary>
        /// Set or Get value for change visibility of string panel
        /// </summary>
        public Visibility StringDataVisibility
        {
            get => (Visibility)GetValue(StringDataVisibilityProperty);
            set => SetValue(StringDataVisibilityProperty, value);
        }

        public static readonly DependencyProperty StringDataVisibilityProperty =
            DependencyProperty.Register("StringDataVisibility", typeof(Visibility), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    new PropertyChangedCallback(StringDataVisibility_ValidateValue),
                    new CoerceValueCallback(Visibility_CoerceValue)));

        private static void StringDataVisibility_ValidateValue(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
            {
                Visibility value = (Visibility)e.NewValue;

                switch (value)
                {
                    case Visibility.Visible:
                        ctrl.StringDataStackPanel.Visibility = Visibility.Visible;
                        break;
                    case Visibility.Collapsed:
                        ctrl.StringDataStackPanel.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        /// <summary>
        /// Set or Get value for change visibility of status bar
        /// </summary>
        public Visibility StatusBarVisibility
        {
            get => (Visibility)GetValue(StatusBarVisibilityProperty);
            set => SetValue(StatusBarVisibilityProperty, value);
        }

        public static readonly DependencyProperty StatusBarVisibilityProperty =
            DependencyProperty.Register("StatusBarVisibility", typeof(Visibility), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    new PropertyChangedCallback(StatusBarVisibility_ValueChange),
                    new CoerceValueCallback(Visibility_CoerceValue)));

        private static void StatusBarVisibility_ValueChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
            {
                Visibility value = (Visibility)e.NewValue;

                switch (value)
                {
                    case Visibility.Visible:
                        ctrl.StatusBarGrid.Visibility = Visibility.Visible;
                        break;

                    case Visibility.Collapsed:
                        ctrl.StatusBarGrid.Visibility = Visibility.Collapsed;
                        break;
                }

                ctrl.RefreshView(false);
            }
        }
        #endregion Visibility property

        #region Undo / Redo

        /// <summary>
        /// Clear undo and change
        /// </summary>
        public void ClearAllChange()
        {
            if (ByteProvider.CheckIsOpen(_provider))
                _provider.ClearUndoChange();
        }

        /// <summary>
        /// Make undo of last the last bytemodified
        /// </summary>
        public void Undo(int repeat = 1)
        {
            UnSelectAll();

            if (ByteProvider.CheckIsOpen(_provider))
            {
                for (int i = 0; i < repeat; i++)
                    _provider.Undo();

                RefreshView(false, true);
            }
        }

        /// <summary>
        /// NOT COMPLETED : Clear the scroll marker when undone 
        /// </summary>
        /// <param name="sender">List of long representing position in file are undone</param>
        /// <param name="e"></param>
        private void Provider_Undone(object sender, EventArgs e)
        {
            if (sender is List<long> bytePosition)
                foreach (long position in bytePosition)
                    ClearScrollMarker(position);
        }

        /// <summary>
        /// Get the undo count
        /// </summary>
        public long UndoCount
        {
            get
            {
                if (ByteProvider.CheckIsOpen(_provider))
                    return _provider.UndoCount;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Get the undo stack
        /// </summary>
        public Stack<ByteModified> UndoStack
        {
            get
            {
                if (ByteProvider.CheckIsOpen(_provider))
                    return _provider.UndoStack;
                else
                    return null;
            }
        }

        #endregion Undo / Redo

        #region Open, Close, Save, byte provider ...

        private void Provider_ChangesSubmited(object sender, EventArgs e)
        {
            if (sender is ByteProvider bp)
            {
                //Refresh filename
                var filename = bp.FileName;
                CloseProvider();
                FileName = filename;

                ChangesSubmited?.Invoke(this, new EventArgs());
            }
        }

        private void ProviderStream_ChangesSubmited(object sender, EventArgs e)
        {
            //Refresh stream
            if (ByteProvider.CheckIsOpen(_provider))
            {
                MemoryStream stream = new MemoryStream(_provider.Stream.ToArray());
                CloseProvider();
                OpenStream(stream);

                ChangesSubmited?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Set or Get the file with the control will show hex
        /// </summary>
        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        // Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(nameof(FileName), typeof(string), typeof(HexaEditor),
                new FrameworkPropertyMetadata("",
                    new PropertyChangedCallback(FileName_PropertyChanged)));

        private static void FileName_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                ctrl.OpenFile((string)e.NewValue);
        }

        /// <summary>
        /// Set the MemoryStream are used by ByteProvider
        /// </summary>
        public MemoryStream Stream
        {
            get => (MemoryStream)GetValue(StreamProperty);
            set => SetValue(StreamProperty, value);
        }

        // Using a DependencyProperty as the backing store for Stream.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StreamProperty =
            DependencyProperty.Register(nameof(Stream), typeof(MemoryStream), typeof(HexaEditor),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(Stream_PropertyChanged)));

        private static void Stream_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
            {
                ctrl.CloseProvider();
                ctrl.OpenStream((MemoryStream)e.NewValue);
            }
        }

        /// <summary>
        /// Close file and clear control
        /// ReadOnlyMode is reset to false
        /// </summary>
        public void CloseProvider()
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                FileName = string.Empty;
                ReadOnlyMode = false;
                VerticalScrollBar.Value = 0;

                _provider.Close();
            }

            UnHighLightAll();
            ClearAllScrollMarker();
            UnSelectAll();
            RefreshView();
            UpdateHeader();
            UpdateScrollBar();
        }

        /// <summary>
        /// Save to the current stream/file
        /// </summary>
        public void SubmitChanges()
        {
            if (ByteProvider.CheckIsOpen(_provider))
                if (!_provider.ReadOnlyMode)
                    _provider.SubmitChanges();
        }

        /// <summary>
        /// Save as to another file
        /// </summary>
        public void SubmitChanges(string newfilename, bool overwrite = false)
        {
            if (ByteProvider.CheckIsOpen(_provider))
                if (!_provider.ReadOnlyMode)
                    _provider.SubmitChanges(newfilename, overwrite);
        }

        /// <summary>
        /// Open file name
        /// </summary>
        /// <param name="filename"></param>
        private void OpenFile(string filename)
        {
            if (String.IsNullOrEmpty(FileName))
            {
                CloseProvider();
                return;
            }

            if (File.Exists(filename))
            {
                CloseProvider();

                _provider = new ByteProvider(filename);

                if (_provider.IsEmpty)
                {
                    CloseProvider();
                    return;
                }

                _provider.ReadOnlyChanged += Provider_ReadOnlyChanged;
                _provider.DataCopiedToClipboard += Provider_DataCopied;
                _provider.ChangesSubmited += Provider_ChangesSubmited;
                _provider.Undone += Provider_Undone;
                _provider.LongProcessProgressChanged += Provider_LongProcessProgressChanged;
                _provider.LongProcessProgressStarted += Provider_LongProcessProgressStarted;
                _provider.LongProcessProgressCompleted += Provider_LongProcessProgressCompleted;
                _provider.LongProcessProgressCanceled += Provider_LongProcessProgressCompleted; //TODO : implement cancel event
                _provider.FillWithByteCompleted += Provider_FillWithByteCompleted;
                _provider.ReplaceByteCompleted += Provider_ReplaceByteCompleted;

                UpdateScrollBar();
                UpdateHeader();

                //Load file with ASCII character table;
                var previousTable = TypeOfCharacterTable;
                TypeOfCharacterTable = CharacterTableType.ASCII;

                RefreshView(true);

                //Replace previous character table
                TypeOfCharacterTable = previousTable;

                UnSelectAll();

                UpdateTBLBookMark();
                UpdateSelectionColor(FirstColor.HexByteData);

                //Update count of byte on file open
                UpdateByteCount();
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        /// <summary>
        /// Open file name
        /// </summary>
        private void OpenStream(MemoryStream stream)
        {
            if (stream.CanRead)
            {
                CloseProvider();

                _provider = new ByteProvider(stream);

                if (_provider.IsEmpty)
                {
                    CloseProvider();
                    return;
                }

                _provider.ReadOnlyChanged += Provider_ReadOnlyChanged;
                _provider.DataCopiedToClipboard += Provider_DataCopied;
                _provider.ChangesSubmited += Provider_ChangesSubmited;
                _provider.Undone += Provider_Undone;
                _provider.LongProcessProgressChanged += Provider_LongProcessProgressChanged;
                _provider.LongProcessProgressStarted += Provider_LongProcessProgressStarted;
                _provider.LongProcessProgressCompleted += Provider_LongProcessProgressCompleted;
                _provider.LongProcessProgressCanceled += Provider_LongProcessProgressCompleted; //TODO : implement cancel event
                _provider.FillWithByteCompleted += Provider_FillWithByteCompleted;
                _provider.ReplaceByteCompleted += Provider_ReplaceByteCompleted;

                UpdateScrollBar();
                UpdateHeader();

                RefreshView(true);

                UnSelectAll();

                UpdateTBLBookMark();
                UpdateSelectionColor(FirstColor.HexByteData);

                //Update count of byte
                UpdateByteCount();
            }
            else
            {
                //throw new Exception("Can't read MemoryStream");
            }
        }

        private void Provider_LongProcessProgressCompleted(object sender, EventArgs e)
        {
            LongProgressProgressBar.Visibility = Visibility.Collapsed;
            CancelLongProcessButton.Visibility = Visibility.Collapsed;

            TraverseHexBytes(ctrl => ctrl.IsEnabled = true);
            TraverseStringBytes(ctrl => ctrl.IsEnabled = true);

            LongProcessProgressCompleted?.Invoke(this, new EventArgs());
        }

        private void Provider_LongProcessProgressStarted(object sender, EventArgs e)
        {
            LongProgressProgressBar.Visibility = Visibility.Visible;
            CancelLongProcessButton.Visibility = Visibility.Visible;

            TraverseHexBytes(ctrl => ctrl.IsEnabled = false);
            TraverseStringBytes(ctrl => ctrl.IsEnabled = false);

            LongProcessProgressStarted?.Invoke(this, new EventArgs());
        }

        private void Provider_LongProcessProgressChanged(object sender, EventArgs e)
        {
            //Update progress bar
            LongProgressProgressBar.Value = (double)sender;
            Application.Current.DoEvents();

            LongProcessProgressChanged?.Invoke(this, new EventArgs());
        }

        private void Provider_ReplaceByteCompleted(object sender, EventArgs e) => ReplaceByteCompleted?.Invoke(this, new EventArgs());

        private void Provider_FillWithByteCompleted(object sender, EventArgs e) => FillWithByteCompleted?.Invoke(this, new EventArgs());

        private void CancelLongProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (ByteProvider.CheckIsOpen(_provider))
                _provider.IsOnLongProcess = false;
        }

        /// <summary>
        /// Check if byteprovider is on long progress and update control
        /// </summary>
        private void CheckProviderIsOnProgress()
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                if (!_provider.IsOnLongProcess)
                {
                    CancelLongProcessButton.Visibility = Visibility.Collapsed;
                    LongProgressProgressBar.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                CancelLongProcessButton.Visibility = Visibility.Collapsed;
                LongProgressProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        #endregion Open, Close, Save, byte provider ...

        #region Traverse IByteControl methods

        /// <summary>
        /// Used to make action on all hexbytecontrol
        /// </summary>
        private void TraverseHexBytes(Action<HexByte> act)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //HexByte panel
            foreach (StackPanel hexDataStack in HexDataStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;
                foreach (HexByte byteControl in hexDataStack.Children.Cast<object>().Where(ctrl => ctrl.GetType() == typeof(HexByte)))
                    act(byteControl);
            }
        }

        /// <summary>
        /// Used to make action on all stringbyte control
        /// </summary>
        private void TraverseStringBytes(Action<StringByte> act)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //Stringbyte panel
            foreach (StackPanel stringDataStack in StringDataStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;
                foreach (StringByte sbControl in stringDataStack.Children.Cast<object>().Where(ctrl => ctrl.GetType() == typeof(StringByte)))
                    act(sbControl);
            }
        }

        /// <summary>
        /// To reduce code.traverse hexbyte and stringbyte controls.
        /// </summary>
        private void TraverseHexAndStringBytes(Action<IByteControl> act)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //Stringbyte panel
            foreach (StackPanel stringDataStack in StringDataStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;
                foreach (StringByte sbControl in stringDataStack.Children.Cast<object>().Where(ctrl => ctrl.GetType() == typeof(StringByte)))
                    act(sbControl);
            }

            //HexByte panel
            cnt = 0;
            foreach (StackPanel hexDataStack in HexDataStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;
                foreach (HexByte byteControl in hexDataStack.Children.Cast<object>().Where(ctrl => ctrl.GetType() == typeof(HexByte)))
                    act(byteControl);
            }
        }
        #endregion Traverse IByteControl methods

        #region Update/Refresh view

        /// <summary>
        /// Get or set the number of byte are show in control
        /// </summary>
        public int BytePerLine
        {
            get => (int)GetValue(BytePerLineProperty);
            set => SetValue(BytePerLineProperty, value);
        }

        public static readonly DependencyProperty BytePerLineProperty =
            DependencyProperty.Register("BytePerLine", typeof(int), typeof(HexaEditor),
                new FrameworkPropertyMetadata(16, new PropertyChangedCallback(BytePerLine_PropertyChanged),
                    new CoerceValueCallback(BytePerLine_CoerceValue)));

        private static object BytePerLine_CoerceValue(DependencyObject d, object baseValue)
        {
            var value = (int)baseValue;

            if (value < 8)
                return 8;
            else if (value > 32)
                return 32;
            else
                return baseValue;
        }

        private static void BytePerLine_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                if (e.NewValue != e.OldValue)
                {
                    ctrl.UpdateScrollBar();
                    ctrl.BuildDataLines((int)ctrl.MaxVisibleLine, true);
                    ctrl.RefreshView(true);
                    ctrl.UpdateHeader();
                }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged) RefreshView(true);
        }

        private void VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshView(false);

        /// <summary>
        /// Update vertical scrollbar with file info
        /// </summary>
        private void UpdateScrollBar()
        {
            VerticalScrollBar.Visibility = Visibility.Collapsed;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                VerticalScrollBar.Visibility = Visibility.Visible;
                VerticalScrollBar.SmallChange = 1;
                VerticalScrollBar.LargeChange = ScrollLargeChange;
                VerticalScrollBar.Maximum = MaxLine- 1;
            }
        }

        /// <summary>
        /// Update de SelectionLine property
        /// </summary>
        private void UpdateSelectionLine()
        {
            if (ByteProvider.CheckIsOpen(_provider))
                SelectionLine = (SelectionStart / BytePerLine) + 1;
            else
                SelectionLine = 0;
        }

        /// <summary>
        /// Refresh currentview of hexeditor
        /// </summary>
        /// <param name="ControlResize"></param>
        public void RefreshView(bool ControlResize = false, bool RefreshData = true)
        {
#if DEBUG
            var start = DateTime.Now;
#endif

            UpdateLinesOffSet();

            if (RefreshData)
                UpdateViewers(ControlResize);

            //Update visual of byte control
            UpdateByteModified();
            UpdateSelection();
            UpdateHighLight();
            UpdateStatusBar();
            UpdateVisual();

            CheckProviderIsOnProgress();

            if (ControlResize)
            {
                UpdateScrollMarkerPosition();
                UpdateHeader();
            }
#if DEBUG
            Debug.Print($"REFRESH TIME MS: {(DateTime.Now - start).Milliseconds}");
#endif
        }

        /// <summary>
        /// Update the selection of byte in hexadecimal panel
        /// </summary>
        private void UpdateSelectionColor(FirstColor coloring)
        {
            switch (coloring)
            {
                case FirstColor.HexByteData:
                    TraverseHexBytes(ctrl => { ctrl.FirstSelected = true; });
                    TraverseStringBytes(ctrl => { ctrl.FirstSelected = false; });
                    break;
                case FirstColor.StringByteData:
                    TraverseHexBytes(ctrl => { ctrl.FirstSelected = false; });
                    TraverseStringBytes(ctrl => { ctrl.FirstSelected = true; });
                    break;
            }
        }

        /// <summary>
        /// Build the stringbyte and hexabyte control used byte hexeditor
        /// </summary>
        /// <param name="maxline">Number of line to build</param>
        private void BuildDataLines(int maxline, bool rebuild = false)
        {
            bool reAttachEvents = false;

            if (rebuild)
            {
                reAttachEvents = true;
                StringDataStackPanel.Children.Clear();
                HexDataStackPanel.Children.Clear();
            }

            HexByte byteControl;
            StringByte sbCtrl;
            for (int lineIndex = StringDataStackPanel.Children.Count; lineIndex < maxline; lineIndex++)
            {
                #region Build StringByte

                StackPanel dataLineStack = new StackPanel()
                {
                    Height = LineHeight,
                    Orientation = Orientation.Horizontal
                };
                
                for (int i = 0; i < BytePerLine; i++)
                {
                    if (_TBLCharacterTable == null)
                        if (ByteSpacerPositioning == ByteSpacerPosition.Both || ByteSpacerPositioning == ByteSpacerPosition.StringBytePanel)
                        AddByteSpacer(dataLineStack, i);

                    sbCtrl = new StringByte(this)
                    {
                        InternalChange = true,
                        ReadOnlyMode = ReadOnlyMode,
                        TBLCharacterTable = _TBLCharacterTable,
                        TypeOfCharacterTable = TypeOfCharacterTable,
                        Byte = null,
                        ByteNext = null,
                        BytePositionInFile = -1
                    };

                    sbCtrl.InternalChange = false;

                    dataLineStack.Children.Add(sbCtrl);
                }
                StringDataStackPanel.Children.Add(dataLineStack);
                #endregion

                #region Build HexByte
                StackPanel hexaDataLineStack = new StackPanel()
                {
                    Height = LineHeight,
                    Orientation = Orientation.Horizontal
                };

                for (int i = 0; i < BytePerLine; i++)
                {
                    if (ByteSpacerPositioning == ByteSpacerPosition.Both || ByteSpacerPositioning == ByteSpacerPosition.HexBytePanel)
                        AddByteSpacer(hexaDataLineStack, i);

                    byteControl = new HexByte(this)
                    {
                        InternalChange = true,
                        ReadOnlyMode = ReadOnlyMode,
                        ToolTip = TryFindResource("ByteToolTip"),
                        Byte = null,
                        BytePositionInFile = -1
                    };

                    byteControl.InternalChange = false;

                    hexaDataLineStack.Children.Add(byteControl);
                }

                HexDataStackPanel.Children.Add(hexaDataLineStack);
                #endregion

                reAttachEvents = true;
            }

            #region Attach/detach events to each IByteControl
            if (reAttachEvents)
                TraverseHexAndStringBytes(ctrl =>
                {
                    #region Detach events
                    ctrl.ByteModified -= Control_ByteModified;
                    ctrl.MoveNext -= Control_MoveNext;
                    ctrl.MovePrevious -= Control_MovePrevious;
                    ctrl.MouseSelection -= Control_MouseSelection;
                    ctrl.Click -= Control_Click;
                    ctrl.RightClick -= Control_RightClick;
                    ctrl.MoveUp -= Control_MoveUp;
                    ctrl.MoveDown -= Control_MoveDown;
                    ctrl.MoveLeft -= Control_MoveLeft;
                    ctrl.MoveRight -= Control_MoveRight;
                    ctrl.MovePageDown -= Control_MovePageDown;
                    ctrl.MovePageUp -= Control_MovePageUp;
                    ctrl.ByteDeleted -= Control_ByteDeleted;
                    ctrl.EscapeKey -= Control_EscapeKey;
                    ctrl.CTRLAKey -= Control_CTRLAKey;
                    ctrl.CTRLZKey -= Control_CTRLZKey;
                    ctrl.CTRLCKey -= Control_CTRLCKey;
                    ctrl.CTRLVKey -= Control_CTRLVKey;
                    #endregion

                    #region Attach events
                    ctrl.ByteModified += Control_ByteModified;
                    ctrl.MoveNext += Control_MoveNext;
                    ctrl.MovePrevious += Control_MovePrevious;
                    ctrl.MouseSelection += Control_MouseSelection;
                    ctrl.Click += Control_Click;
                    ctrl.RightClick += Control_RightClick;
                    ctrl.MoveUp += Control_MoveUp;
                    ctrl.MoveDown += Control_MoveDown;
                    ctrl.MoveLeft += Control_MoveLeft;
                    ctrl.MoveRight += Control_MoveRight;
                    ctrl.MovePageDown += Control_MovePageDown;
                    ctrl.MovePageUp += Control_MovePageUp;
                    ctrl.ByteDeleted += Control_ByteDeleted;
                    ctrl.EscapeKey += Control_EscapeKey;
                    ctrl.CTRLAKey += Control_CTRLAKey;
                    ctrl.CTRLZKey += Control_CTRLZKey;
                    ctrl.CTRLCKey += Control_CTRLCKey;
                    ctrl.CTRLVKey += Control_CTRLVKey;
                    #endregion
                });
            #endregion
        }
        
        /// <summary>
        /// Update the data and string stackpanels yo current view;
        /// </summary>
        private void UpdateViewers(bool ControlResize)
        {
            var curLevel = ++_priLevel;
            if (ByteProvider.CheckIsOpen(_provider))
            {
                if (ControlResize)
                {
                    #region 
                    if (_viewBuffer == null)
                    {
                        var fullSizeReadyToRead = MaxVisibleLine* BytePerLine + 1;
                        _viewBuffer = new byte[fullSizeReadyToRead];
                        BuildDataLines((int)MaxVisibleLine);
                    }
                    else
                    {
                        if (_viewBuffer.Length < MaxVisibleLine* BytePerLine + 1)
                        {
                            var fullSizeReadyToRead = MaxVisibleLine* BytePerLine + 1;
                            BuildDataLines((int)MaxVisibleLine);
                            _viewBuffer = new byte[fullSizeReadyToRead];
                        }
                    }
                    #endregion
                }

                if (LinesInfoStackPanel.Children.Count == 0)
                    return;

                var firstInfoLabel = LinesInfoStackPanel.Children[0] as TextBlock;
                var startPosition = ByteConverters.HexLiteralToLong(firstInfoLabel.Text).position;
                var sizeReadyToRead = LinesInfoStackPanel.Children.Count * BytePerLine + 1;
                _provider.Position = startPosition;
                var readSize = _provider.Read(_viewBuffer, 0, sizeReadyToRead);
                var index = 0;

                #region
                TraverseHexBytes(byteControl =>
                {
                    byteControl.Action = ByteAction.Nothing;
                    byteControl.ReadOnlyMode = ReadOnlyMode;

                    if (byteControl.ToolTip == null)
                        byteControl.ToolTip = TryFindResource("ByteToolTip");

                    byteControl.InternalChange = true;

                    if (index < readSize && _priLevel == curLevel)
                    {
                        byteControl.Byte = _viewBuffer[index];
                        byteControl.BytePositionInFile = startPosition + index;
                    }
                    else
                    {
                        byteControl.Byte = null;
                        byteControl.BytePositionInFile = -1;
                    }
                    byteControl.InternalChange = false;
                    index++;
                });
                #endregion

                index = 0;
                #region
                TraverseStringBytes(sbCtrl =>
                {
                    sbCtrl.Action = ByteAction.Nothing;
                    sbCtrl.ReadOnlyMode = ReadOnlyMode;

                    if (sbCtrl.ToolTip == null)
                        sbCtrl.ToolTip = TryFindResource("ByteToolTip");

                    sbCtrl.InternalChange = true;
                    sbCtrl.TBLCharacterTable = _TBLCharacterTable;
                    sbCtrl.TypeOfCharacterTable = TypeOfCharacterTable;

                    if (index < readSize)
                    {
                        sbCtrl.Byte = _viewBuffer[index];
                        sbCtrl.BytePositionInFile = startPosition + index;
                        if (index < readSize - 1)
                        {
                            sbCtrl.ByteNext = _viewBuffer[index + 1];
                        }
                        else
                        {
                            sbCtrl.ByteNext = null;
                        }
                    }
                    else
                    {
                        sbCtrl.Byte = null;
                        sbCtrl.ByteNext = null;
                        sbCtrl.BytePositionInFile = -1;
                    }
                    sbCtrl.InternalChange = false;
                    index++;
                });
                #endregion

            }
            else
            {
                #region Clear IByteControl
                _viewBuffer = null;
                TraverseHexBytes(control =>
                {
                    control.BytePositionInFile = -1;
                    control.Byte = null;
                    control.Action = ByteAction.Nothing;
                    control.IsHighLight = false;
                    control.IsFocus = false;
                    control.IsSelected = false;
                    control.ToolTip = null;
                });

                TraverseStringBytes(control =>
                {
                    control.BytePositionInFile = -1;
                    control.Byte = null;
                    control.Action = ByteAction.Nothing;
                    control.ByteNext = null;
                    control.IsHighLight = false;
                    control.IsSelected = false;
                    control.ToolTip = null;
                });
                #endregion
            }
        }

        /// <summary>
        /// Update byte are modified
        /// </summary>
        private void UpdateByteModified()
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                var ModifiedBytesDictionary = _provider.GetModifiedBytes(ByteAction.All);

                TraverseHexAndStringBytes(ctrl =>
                {
                    if (ModifiedBytesDictionary.TryGetValue(ctrl.BytePositionInFile, out ByteModified byteModified))
                    {
                        ctrl.InternalChange = true;
                        ctrl.Byte = byteModified.Byte;

                        if (byteModified.Action == ByteAction.Modified || byteModified.Action == ByteAction.Deleted)
                            ctrl.Action = byteModified.Action;

                        ctrl.InternalChange = false;
                    }
                });

            }
        }

        /// <summary>
        /// Update the selection of byte
        /// </summary>
        private void UpdateSelection()
        {
            long minSelect = SelectionStart <= SelectionStop ? SelectionStart : SelectionStop;
            long maxSelect = SelectionStart <= SelectionStop ? SelectionStop : SelectionStart;

            TraverseHexAndStringBytes(ctrl =>
            {
                if (ctrl.BytePositionInFile >= minSelect &&
                        ctrl.BytePositionInFile <= maxSelect &&
                        ctrl.BytePositionInFile != -1)
                    ctrl.IsSelected = ctrl.Action == ByteAction.Deleted ? false : true;
                else
                    ctrl.IsSelected = false;
            });
        }

        /// <summary>
        /// Update bytes as marked on findall()
        /// </summary>
        private void UpdateHighLight()
        {
            bool find = false;

            if (_markedPositionList.Count > 0)
            {
                TraverseHexAndStringBytes(Control =>
                {
                    find = false;

                    foreach (long position in _markedPositionList)
                        if (position == Control.BytePositionInFile)
                        {
                            find = true;
                            break;
                        }

                    Control.IsHighLight = find;
                });
            }
            else //Un highlight all            
                TraverseHexAndStringBytes(Control => { Control.IsHighLight = false; });
        }

        /// <summary>
        /// Update the position info panel at left of the control
        /// </summary>
        private void UpdateHeader()
        {
            HexHeaderStackPanel.Children.Clear();

            if (ByteProvider.CheckIsOpen(_provider))
                for (int i = 0; i < BytePerLine; i++)
                {
                    if (ByteSpacerPositioning == ByteSpacerPosition.Both || ByteSpacerPositioning == ByteSpacerPosition.HexBytePanel)
                        AddByteSpacer(HexHeaderStackPanel, i);

                    //Create control
                    TextBlock LineInfoLabel = new TextBlock()
                    {
                        Height = LineHeight,
                        Padding = new Thickness(0, 0, 10, 0),
                        Foreground = ForegroundOffSetHeaderColor,
                        Width = 20,
                        TextAlignment = TextAlignment.Center,
                        Text = ByteConverters.ByteToHex((byte)i),
                        ToolTip = $"Column : {i.ToString()}",
                        FontFamily = FontFamily
                    };

                    HexHeaderStackPanel.Children.Add(LineInfoLabel);
                }
        }

        /// <summary>
        /// Update the position info panel at left of the control
        /// </summary>
        private void UpdateLinesOffSet()
        {
            LinesInfoStackPanel.Children.Clear();

            long fds = MaxVisibleLine;

            //If the lines are less than "visible lines" create them;
            var linesCount = LinesInfoStackPanel.Children.Count;

            if (linesCount < fds)
            {
                for (int i = 0; i < fds - linesCount; i++)
                {
                    var LineInfoLabel = new TextBlock()
                    {
                        Height = LineHeight,
                        Padding = new Thickness(0, 0, 10, 0),
                        Foreground = ForegroundOffSetHeaderColor,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Left,
                        FontFamily = FontFamily
                    };

                    //Events
                    LineInfoLabel.MouseDown += LineInfoLabel_MouseDown;
                    LineInfoLabel.MouseMove += LineInfoLabel_MouseMove;

                    LinesInfoStackPanel.Children.Add(LineInfoLabel);
                }

            }

            if (ByteProvider.CheckIsOpen(_provider))
            {
                for (int i = 0; i < fds; i++)
                {
                    long firstLineByte = ((long)VerticalScrollBar.Value + i) * BytePerLine;
                    var LineInfoLabel = (TextBlock)LinesInfoStackPanel.Children[i];

                    if (firstLineByte < _provider.Length)
                    {
                        //Create control
                        LineInfoLabel.Text = "0x" + ByteConverters.LongToHex(firstLineByte).ToUpper();
                        LineInfoLabel.ToolTip = $"Byte : {firstLineByte.ToString()}";
                    }
                }
            }
        }


        /// <summary>
        /// Get first visible byte position in control
        /// </summary>
        /// <returns>Return -1 of no file open</returns>
        private long FirstVisibleBytePosition
        {
            get
            {
                long rtn = -1;
                int count = 0;
                TraverseHexBytes(ctrl =>
                {
                    count++;

                    if (count == 1)
                        rtn = ctrl.BytePositionInFile;
                });

                return rtn;
            }
        }

        /// <summary>
        /// Return True if SelectionStart are visible in control
        /// </summary>
        public bool SelectionStartIsVisible
        {
            get
            {
                bool rtn = false;
                TraverseHexBytes(ctrl =>
                {
                    if (ctrl.BytePositionInFile == SelectionStart)
                        rtn = true;
                });

                return rtn;
            }
        }

        /// <summary>
        /// Get last visible byte position in control
        /// </summary>
        /// <returns>Return -1 of no file open.</returns>
        private long LastVisibleBytePosition => FirstVisibleBytePosition+ (MaxVisibleLine- 1) * BytePerLine - 1;

        #endregion First/Last visible byte methods

        #region Focus Methods

        /// <summary>
        /// Set focus on byte
        /// </summary>
        private void SetFocusHexDataPanel(long bytePositionInFile)
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                if (bytePositionInFile >= _provider.Length)
                    return;

                bool rtn = false;
                TraverseHexBytes(ctrl =>
                {
                    if (ctrl.BytePositionInFile == bytePositionInFile)
                    {
                        ctrl.Focus();
                        rtn = true;
                    }
                });

                if (rtn) return;

                if (VerticalScrollBar.Value < VerticalScrollBar.Maximum)                
                    VerticalScrollBar.Value++;
                
                if (!SelectionStartIsVisible&& SelectionLength == 1)
                    SetPosition(SelectionStart, 1);
            }
        }

        /// <summary>
        /// Set focus on byte
        /// </summary>
        private void SetFocusStringDataPanel(long bytePositionInFile)
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                if (bytePositionInFile >= _provider.Length)
                    return;

                bool rtn = false;
                TraverseStringBytes(ctrl =>
                {
                    if (ctrl.BytePositionInFile == bytePositionInFile)
                    {
                        ctrl.Focus();
                        rtn = true;
                    }
                });

                if (rtn) return;

                if (VerticalScrollBar.Value < VerticalScrollBar.Maximum)
                    VerticalScrollBar.Value++;

                if (!SelectionStartIsVisible&& SelectionLength == 1)
                    SetPosition(SelectionStart, 1);
            }
        }
        #endregion Focus Methods

        #region Find methods

        /// <summary>
        /// Find first occurence of string in stream. Search start as startPosition.
        /// </summary>
        public long FindFirst(string text, long startPosition = 0) => FindFirst(ByteConverters.StringToByte(text), startPosition);

        /// <summary>
        /// Find first occurence of byte[] in stream. Search start as startPosition.
        /// </summary>
        public long FindFirst(byte[] data, long startPosition = 0)
        {
            if (data == null) return -1;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                try
                {
                    var position = _provider.FindIndexOf(data, startPosition).First();
                    SetPosition(position, data.Length);
                    return position;
                }
                catch
                {
                    UnSelectAll();
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find next occurence of string in stream search start at SelectionStart.
        /// </summary>
        public long FindNext(string text) => FindNext(ByteConverters.StringToByte(text));

        /// <summary>
        /// Find next occurence of byte[] in stream search start at SelectionStart.
        /// </summary>
        public long FindNext(byte[] data)
        {
            if (data == null) return -1;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                try
                {
                    var position = _provider.FindIndexOf(data, SelectionStart + 1).First();
                    SetPosition(position, data.Length);
                    return position;
                }
                catch
                {
                    UnSelectAll();
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find last occurence of string in stream search start at SelectionStart.
        /// </summary>
        public long FindLast(string text) => FindLast(ByteConverters.StringToByte(text));

        /// <summary>
        /// Find first occurence of byte[] in stream.
        /// </summary>
        public long FindLast(byte[] data)
        {
            if (data == null) return -1;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                try
                {
                    var position = _provider.FindIndexOf(data, SelectionStart + 1).Last();
                    SetPosition(position, data.Length);
                    return position;
                }
                catch
                {
                    UnSelectAll();
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find all occurence of string in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(string text) => FindAll(ByteConverters.StringToByte(text));

        /// <summary>
        /// Find all occurence of byte[] in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(byte[] data)
        {
            if (data == null) return null;

            UnHighLightAll();

            if (ByteProvider.CheckIsOpen(_provider))
                return _provider.FindIndexOf(data, 0);

            return null;
        }

        /// <summary>
        /// Find all occurence of string in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(string text, bool highLight) => FindAll(ByteConverters.StringToByte(text), highLight);

        /// <summary>
        /// Find all occurence of string in stream. Highlight occurance in stream is MarcAll as true
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(byte[] data, bool highLight)
        {
            if (data == null) return null;

            ClearScrollMarker(ScrollMarker.SearchHighLight);

            if (highLight)
            {
                var positions = FindAll(data);

                foreach (long position in positions)
                {
                    for (long i = position; i < position + data.Length; i++)
                        _markedPositionList.Add(i);

                    SetScrollMarker(position, ScrollMarker.SearchHighLight);
                }

                UnSelectAll();
                UpdateHighLight();

                //Sort list
                _markedPositionList.Sort();

                return positions;
            }
            else
                return FindAll(data);
        }

        /// <summary>
        /// Find all occurence of SelectionByteArray in stream. Highlight byte finded
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAllSelection(bool highLight)
        {
            if (SelectionLength > 0)
                return FindAll(SelectionByteArray, highLight);
            else
                return null;
        }

        #endregion Find methods

        #region Statusbar
        /// <summary>
        /// Update statusbar for somes property dont support dependency property
        /// </summary>
        private void UpdateStatusBar()
        {
            if (StatusBarVisibility == Visibility.Visible)
                if (ByteProvider.CheckIsOpen(_provider))
                {
                    #region Show lenght
                    bool MB = false;
                    long deletedBytesCount = _provider.GetModifiedBytes(ByteAction.Deleted).Count();
                    long addedBytesCount = _provider.GetModifiedBytes(ByteAction.Added).Count();

                    FileLengthLabel.Content = _provider.Length - deletedBytesCount;

                    //is mega bytes ?
                    double lenght = (_provider.Length - deletedBytesCount + addedBytesCount) / 1024;

                    if (lenght > 1024)
                    {
                        lenght = lenght / 1024;
                        MB = true;
                    }

                    FileLengthKBLabel.Content = Math.Round(lenght, 2) + (MB == true ? " MB" : " KB");
                    #endregion

                    #region Byte count of selectionStart

                    if (AllowByteCount == true && _bytecount != null && SelectionStart > -1)
                    {
                        ByteCountPanel.Visibility = Visibility.Visible;

                        var val = _provider.GetByte(SelectionStart).Value;
                        CountOfByteSumLabel.Content = _bytecount[val];
                        CountOfByteLabel.Content = $"0x{ByteConverters.LongToHex(val)}";
                    }
                    else
                    {
                        ByteCountPanel.Visibility = Visibility.Collapsed;
                    }
                    #endregion
                }
                else
                {
                    FileLengthLabel.Content = 0;
                    FileLengthKBLabel.Content = 0;
                    CountOfByteLabel.Content = 0;
                }
        }

        #endregion Statusbar

        #region Bookmark and other scrollmarker
        /// <summary>
        /// Get scrollmarkers 
        /// </summary>        
        private IEnumerable<BookMark> GetScrollMarkers(ScrollMarker sm)
        {
            foreach (Rectangle rc in MarkerGrid.Children)
                if (rc.Tag is BookMark bm)
                    if (bm.Marker == sm)
                        yield return bm;
        }

        /// <summary>
        /// Get all bookmark are currently set
        /// </summary>
        public IEnumerable<BookMark> BookMarks
        {
            get
            {
                foreach (Rectangle rc in MarkerGrid.Children)
                    if (rc.Tag is BookMark bm)
                        if (bm.Marker == ScrollMarker.Bookmark)
                            yield return bm;
            }
        }

        /// <summary>
        /// Set bookmark at specified position
        /// </summary>
        /// <param name="position"></param>
        public void SetBookMark(long position) => SetScrollMarker(position, ScrollMarker.Bookmark);

        /// <summary>
        /// Set bookmark at selection start
        /// </summary>
        public void SetBookMark() => SetScrollMarker(SelectionStart, ScrollMarker.Bookmark);

        /// <summary>
        /// Set marker at position using bookmark object
        /// </summary>
        /// <param name="mark"></param>
        private void SetScrollMarker(BookMark mark) => SetScrollMarker(mark.BytePositionInFile, mark.Marker, mark.Description);

        /// <summary>
        /// Set marker at position
        /// </summary>
        private void SetScrollMarker(long position, ScrollMarker marker, string description = "")
        {
            Rectangle rect = new Rectangle();
            double topPosition = 0;
            double rightPosition = 0;

            //create bookmark
            var bookMark = new BookMark()
            {
                Marker = marker,
                BytePositionInFile = position,
                Description = description
            };

            //Remove selection start marker and set position
            if (marker == ScrollMarker.SelectionStart)
            {
                int i = 0;
                foreach (Rectangle ctrl in MarkerGrid.Children)
                {
                    if (((BookMark)ctrl.Tag).Marker == ScrollMarker.SelectionStart)
                    {
                        MarkerGrid.Children.RemoveAt(i);
                        break;
                    }
                    i++;
                }

                bookMark.BytePositionInFile = SelectionStart;
            }

            //Set position in scrollbar
            topPosition = (GetLineNumber(bookMark.BytePositionInFile) * VerticalScrollBar.Track.TickHeight(MaxLine) - 1);

            if (double.IsNaN(topPosition))
                topPosition = 0;

            //Check if position already exist and exit if exist
            if (marker != ScrollMarker.SelectionStart)
                foreach (Rectangle ctrl in MarkerGrid.Children)
                    if (ctrl.Margin.Top == topPosition && ((BookMark)ctrl.Tag).Marker == marker)
                        return;

            //Somes general properties
            rect.MouseDown += Rect_MouseDown;
            rect.VerticalAlignment = VerticalAlignment.Top;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.Tag = bookMark;
            rect.Width = 5;
            rect.Height = 3;

            var byteinfo = new ByteModified() { BytePositionInFile = position };
            rect.DataContext = byteinfo;

            //Set somes properties for different marker
            switch (marker)
            {
                case ScrollMarker.TBLBookmark:
                case ScrollMarker.Bookmark:
                    rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                    rect.Fill = (SolidColorBrush)TryFindResource("BookMarkColor");
                    break;
                case ScrollMarker.SearchHighLight:
                    rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                    rect.Fill = (SolidColorBrush)TryFindResource("SearchBookMarkColor");
                    rect.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
                case ScrollMarker.SelectionStart:
                    rect.Fill = (SolidColorBrush)TryFindResource("SelectionStartBookMarkColor");
                    rect.Width = VerticalScrollBar.ActualWidth;
                    rect.Height = 2;
                    break;
                case ScrollMarker.ByteModified:
                    rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                    rect.Fill = (SolidColorBrush)TryFindResource("ByteModifiedMarkColor");
                    rect.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                case ScrollMarker.ByteDeleted:
                    rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                    rect.Fill = (SolidColorBrush)TryFindResource("ByteDeletedMarkColor");
                    rect.HorizontalAlignment = HorizontalAlignment.Right;
                    rightPosition = 4;
                    break;
            }

            try
            {
                rect.Margin = new Thickness(0, topPosition, rightPosition, 0);
            }
            catch { }

            //Add to grid
            if (ByteProvider.CheckIsOpen(_provider))
                MarkerGrid.Children.Add(rect);
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rect)
                if (rect.Tag is BookMark bm)
                {
                    if (bm.Marker != ScrollMarker.SelectionStart)
                        SetPosition(bm.BytePositionInFile, 1);
                    else
                        SetPosition(SelectionStart, 1);
                }
        }

        /// <summary>
        /// Update all scroll marker position
        /// </summary>
        private void UpdateScrollMarkerPosition()
        {
            foreach (Rectangle rect in MarkerGrid.Children)
                if (rect.Tag is BookMark bm)
                    if (bm.Marker != ScrollMarker.SelectionStart)
                        rect.Margin = new Thickness
                            (
                                0,
                                (GetLineNumber(bm.BytePositionInFile) * VerticalScrollBar.Track.TickHeight(MaxLine)) - rect.ActualHeight,
                                0,
                                0
                            );
        }

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        public void ClearAllScrollMarker() => MarkerGrid.Children.Clear();

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        /// <param name="marker">Type of marker to clear</param>
        public void ClearScrollMarker(ScrollMarker marker)
        {
            for (int i = 0; i < MarkerGrid.Children.Count; i++)
            {
                BookMark mark = (BookMark)((Rectangle)MarkerGrid.Children[i]).Tag;

                if (mark.Marker == marker)
                {
                    MarkerGrid.Children.Remove(MarkerGrid.Children[i]);
                    i--;
                }
            }
        }

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        /// <param name="marker">Type of marker to clear</param>
        public void ClearScrollMarker(ScrollMarker marker, long position)
        {
            for (int i = 0; i < MarkerGrid.Children.Count; i++)
            {
                BookMark mark = (BookMark)((Rectangle)MarkerGrid.Children[i]).Tag;

                if (mark.Marker == marker && mark.BytePositionInFile == position)
                {
                    MarkerGrid.Children.Remove(MarkerGrid.Children[i]);
                    i--;
                }
            }
        }

        /// <summary>
        /// Clear ScrollMarker at position
        /// </summary>
        /// <param name="marker">Type of marker to clear</param>
        public void ClearScrollMarker(long position)
        {
            for (int i = 0; i < MarkerGrid.Children.Count; i++)
            {
                BookMark mark = (BookMark)((Rectangle)MarkerGrid.Children[i]).Tag;

                if (mark.BytePositionInFile == position)
                {
                    MarkerGrid.Children.Remove(MarkerGrid.Children[i]);
                    i--;
                }
            }
        }

        #endregion Bookmark and other scrollmarker

        #region Context menu

        /// <summary>
        /// Allow or not the context menu to appear on right-click
        /// </summary>
        public bool AllowContextMenu { get; set; } = true;

        private void Control_RightClick(object sender, EventArgs e)
        {
            if (AllowContextMenu)
            {
                //position                
                if (sender is IByteControl ctrl)
                    _rightClickBytePosition = ctrl.BytePositionInFile;

                if (SelectionLength <= 1)
                {
                    SelectionStart = _rightClickBytePosition;
                    SelectionStop = _rightClickBytePosition;
                }

                //update ctrl
                CopyAsCMenu.IsEnabled = false;
                CopyASCIICMenu.IsEnabled = false;
                FindAllCMenu.IsEnabled = false;
                CopyHexaCMenu.IsEnabled = false;
                UndoCMenu.IsEnabled = false;
                DeleteCMenu.IsEnabled = false;
                FillByteCMenu.IsEnabled = false;
                CopyTBLCMenu.IsEnabled = false;

                if (SelectionLength > 0)
                {
                    CopyASCIICMenu.IsEnabled = true;
                    CopyAsCMenu.IsEnabled = true;
                    FindAllCMenu.IsEnabled = true;
                    CopyHexaCMenu.IsEnabled = true;
                    DeleteCMenu.IsEnabled = true;
                    FillByteCMenu.IsEnabled = true;

                    if (_TBLCharacterTable != null)
                        CopyTBLCMenu.IsEnabled = true;
                }

                if (UndoCount > 0)
                    UndoCMenu.IsEnabled = true;

                //Show context menu
                Focus();
                CMenu.Visibility = Visibility.Visible;
            }
        }

        private void FindAllCMenu_Click(object sender, RoutedEventArgs e) => FindAll(SelectionByteArray, true);

        private void CopyHexaCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.HexaString);

        private void CopyASCIICMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.ASCIIString);

        private void CopyCSharpCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.CSharpCode);

        private void CopyFSharpCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.FSharp);

        private void CopyVBNetCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.VBNetCode);

        private void CopyCCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.CCode);

        private void CopyJavaCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.JavaCode);

        private void CopyTBLCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.TBLString);

        private void DeleteCMenu_Click(object sender, RoutedEventArgs e) => DeleteSelection();

        private void UndoCMenu_Click(object sender, RoutedEventArgs e) => Undo();

        private void BookMarkCMenu_Click(object sender, RoutedEventArgs e) => SetBookMark(_rightClickBytePosition);

        private void ClearBookMarkCMenu_Click(object sender, RoutedEventArgs e) => ClearScrollMarker(ScrollMarker.Bookmark);

        private void PasteMenu_Click(object sender, RoutedEventArgs e) => PasteWithoutInsert();

        private void SelectAllCMenu_Click(object sender, RoutedEventArgs e) => SelectAll();

        private void FillByteCMenu_Click(object sender, RoutedEventArgs e)
        {
            GiveByteWindow window = new GiveByteWindow()
            {
                Title = "Enter value for fill selection",
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
                if (window.HexTextBox.LongValue <= 255)
                    FillWithByte((byte)window.HexTextBox.LongValue);
        }
        
        private void ReplaceByteCMenu_Click(object sender, RoutedEventArgs e)
        {
            ReplaceByteWindow window = new ReplaceByteWindow()
            {
                Title = "Enter the byte to replace with another.",
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
                if (window.HexTextBox.LongValue <= 255 &&
                    window.ReplaceHexTextBox.LongValue <= 255)
                {
                    ReplaceByte((byte)window.HexTextBox.LongValue, (byte)window.ReplaceHexTextBox.LongValue);
                }
        }

        #endregion Context menu

        #region Bottom and top rectangle

        /// <summary>
        /// Vertical Move Method By Time,
        /// </summary>
        /// <param name="readToMove">whether the veticalbar value should be changed</param>
        /// <param name="distance">the value that vertical value move down(negative for up)</param>
        private void VerticalMoveByTime(Func<bool> readToMove, Func<double> distance)
        {
            ThreadPool.QueueUserWorkItem(cb =>
            {
                while (readToMove())
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (Mouse.LeftButton == MouseButtonState.Pressed)
                        {
                            VerticalScrollBar.Value += distance();

                            //Selection stop
                            if (_mouseOnBottom)
                                SelectionStop = LastVisibleBytePosition;
                            else if (_mouseOnTop)
                                SelectionStop = FirstVisibleBytePosition;
                        }

                        //Give the control to dispatcher for do events
                        Application.Current.DoEvents();

                    });
                }
            });
        }

        private void BottomRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            _mouseOnBottom = true;
            var curTime = ++_bottomEnterTimes;

            VerticalMoveByTime
            (
                () => _mouseOnBottom && curTime == _bottomEnterTimes,

                () => { return (int)MouseWheelSpeed; }
            );
        }

        private void BottomRectangle_MouseLeave(object sender, MouseEventArgs e) => _mouseOnBottom = false;

        private void TopRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            var curTime = ++_topEnterTimes;
            _mouseOnTop = true;

            VerticalMoveByTime
            (
                () => _mouseOnTop && curTime == _topEnterTimes,

                () => { return -(int)MouseWheelSpeed; }
            );
        }

        private void TopRectangle_MouseLeave(object sender, MouseEventArgs e) => _mouseOnTop = false;
        #endregion Bottom and Top rectangle

        #region Highlight selected byte        
        /// <summary>
        /// Byte at selection start
        /// </summary>
        internal byte? SelectionByte { get; set; } = null;

        /// <summary>
        /// Set to true for highlight the same byte are selected in view.
        /// </summary>
        public bool AllowAutoHightLighSelectionByte { get; set; } = true;

        /// <summary>
        /// Brush used to color the selectionbyte
        /// </summary>
        public Brush AutoHighLiteSelectionByteBrush
        {
            get { return (Brush)GetValue(AutoHighLiteSelectionByteBrushProperty); }
            set { SetValue(AutoHighLiteSelectionByteBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoHighLiteSelectionByteBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoHighLiteSelectionByteBrushProperty =
            DependencyProperty.Register(nameof(AutoHighLiteSelectionByteBrush), typeof(Brush), typeof(HexaEditor),
                new FrameworkPropertyMetadata(Brushes.LightBlue,
                    new PropertyChangedCallback(AutoHighLiteSelectionByteBrush_Changed)));

        private static void AutoHighLiteSelectionByteBrush_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                ctrl.UpdateVisual();
        }
        #endregion Highlight selected byte

        #region ByteCount property

        public bool AllowByteCount
        {
            get => (bool)GetValue(AllowByteCountProperty);
            set => SetValue(AllowByteCountProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowByteCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowByteCountProperty =
            DependencyProperty.Register(nameof(AllowByteCount), typeof(bool), typeof(HexaEditor),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(AllowByteCount_PropertyChanged)));

        private static void AllowByteCount_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
            {
                if (e.NewValue != e.OldValue)
                    ctrl.UpdateByteCount();

                ctrl.UpdateStatusBar();
            }
        }

        /// <summary>
        /// Update the bytecount var.
        /// </summary>
        private void UpdateByteCount()
        {
            _bytecount = null;

            if (ByteProvider.CheckIsOpen(_provider))
                if (AllowByteCount) _bytecount = _provider.GetByteCount();
        }

        #endregion ByteCount Property

        #region IDisposable Support
        private bool disposedValue = false; // for detect redondants call

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                //Dispose managed object
                if (disposing)
                {
                    _provider.Dispose();
                    _TBLCharacterTable.Dispose();
                    _viewBuffer = null;
                    _markedPositionList = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);
        #endregion

        #region IByteControl grouping


        public ByteSpacerPosition ByteSpacerPositioning
        {
            get => (ByteSpacerPosition)GetValue(ByteSpacerPositioningProperty);
            set => SetValue(ByteSpacerPositioningProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteSpacerPositioning.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteSpacerPositioningProperty =
            DependencyProperty.Register(nameof(ByteSpacerPositioning), typeof(ByteSpacerPosition), typeof(HexaEditor),
                new FrameworkPropertyMetadata(ByteSpacerPosition.Both, new PropertyChangedCallback(ByteSpacer_Changed)));


        public ByteSpacerWidth ByteSpacerWidthTickness
        {
            get => (ByteSpacerWidth)GetValue(ByteSpacerWidthTicknessProperty);
            set => SetValue(ByteSpacerWidthTicknessProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteSpacer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteSpacerWidthTicknessProperty =
            DependencyProperty.Register(nameof(ByteSpacerWidthTickness), typeof(ByteSpacerWidth), typeof(HexaEditor), 
                new FrameworkPropertyMetadata(ByteSpacerWidth.Normal, new PropertyChangedCallback(ByteSpacer_Changed)));

        private static void ByteSpacer_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexaEditor ctrl)
                ctrl.RefreshView(true);
        }
                
        public ByteSpacerGroup ByteGrouping
        {
            get => (ByteSpacerGroup)GetValue(ByteGroupingProperty);
            set => SetValue(ByteGroupingProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteGrouping.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteGroupingProperty =
            DependencyProperty.Register(nameof(ByteGrouping), typeof(ByteSpacerGroup), typeof(HexaEditor),
                new FrameworkPropertyMetadata(ByteSpacerGroup.EightByte, new PropertyChangedCallback(ByteSpacer_Changed)));

        /// <summary>
        /// Add Spacer
        /// </summary>
        private void AddByteSpacer(StackPanel stack, int colomn)
        {
            if (colomn % (int)ByteGrouping == 0 && colomn > 0)
                stack.Children.Add(new TextBlock() { Width = (int)ByteSpacerWidthTickness });
        }

        #endregion IByteControl grouping
    }
}
