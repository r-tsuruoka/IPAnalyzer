#region using
using MathNet.Numerics;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
#endregion

namespace Crystallography;

[Serializable()]
public class Crystal : IEquatable<Crystal>, ICloneable, IComparable<Crystal>
{
    #region オーバーライド
    public object Clone()
    {
        Crystal crystal = (Crystal)this.MemberwiseClone();
        crystal.Atoms = new Atoms[Atoms.Length];
        for (int i = 0; i < Atoms.Length; i++)
            crystal.Atoms[i] = (Atoms)Atoms[i].Clone();
        //crystal.EOSCondition = this.EOSCondition.Clone();
        crystal.ElasticStiffnessArray = this.ElasticStiffnessArray;

        return crystal;
    }

    public int CompareTo(Crystal o) => Residual.CompareTo(o.Residual);

    #region Equalsオーバーライド
    public bool Equals(Crystal o)
    {
        if (o == null)
            return false;
        if (A == o.A && B == o.B && C == o.C && Alpha == o.Alpha && Beta == o.Beta && Gamma == o.Gamma &&
                   SymmetrySeriesNumber == o.SymmetrySeriesNumber && Name == o.Name && JournalName == o.JournalName && PublSectionTitle == o.PublSectionTitle && JournalName == o.JournalName && ChemicalFormulaSum == o.ChemicalFormulaSum
                   && Density == o.Density)
            if (Atoms.Length == o.Atoms.Length)
                for (int l = 0; l < Atoms.Length; l++)
                    if (Atoms[l].X == o.Atoms[l].X && Atoms[l].Y == o.Atoms[l].Y && Atoms[l].Z == o.Atoms[l].Z &&
                        Atoms[l].Occ == o.Atoms[l].Occ && Atoms[l].Label == o.Atoms[l].Label && Atoms[l].SubNumberElectron == o.Atoms[l].SubNumberElectron)
                        return true;
        return false;

    }
    public override bool Equals(object obj)
    {
        if (obj is Crystal o)
            return Equals(o);
        else
            return false;
    }

    public override int GetHashCode()
    {
        return new { CellValue, Atoms, JournalInformation }.GetHashCode();
    }

    public static bool operator ==(Crystal left, Crystal right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(Crystal left, Crystal right) => !(left == right);

    public static bool operator <(Crystal left, Crystal right) => left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(Crystal left, Crystal right) => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(Crystal left, Crystal right) => left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(Crystal left, Crystal right) => left is null ? right is null : left.CompareTo(right) >= 0;
    #endregion

    public override string ToString() => Name.ToString();

    #endregion

    #region プロパティ、フィールド

    #region PDIndexer関連

    /// <summary>
    /// フレキシブルモード. PDIndexerで利用
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public bool FlexibleMode = false;

    /// <summary>
    /// 保護される結晶であるかどうか. PDIndexerで利用
    /// </summary>
    public bool Reserved = false;

    /// <summary>
    /// 残差. AtomicPositionFinderから呼ばれる。
    /// </summary>
    public double Residual = 0;

    /// <summary>
    /// 全体的な回折強度.  PDIndexerから呼ばれる.
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public double DiffractionPeakIntensity = -1;


    [NonSerialized]
    [XmlIgnore]
    public double Consistency, VolumeRatio, WeightRatio, MolRatio;//fittingFormのところで使用

    #endregion

    #region 結晶面、晶帯軸、逆格子点ベクトル、菊池線ベクトルなど
    /// <summary>
    ///
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public List<Plane> Plane = [];

    public List<Plane> FlexiblePlane = [];

    /// <summary>
    /// 軸ベクトル配列
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public List<Vector3D> VectorOfAxis = [];

    /// <summary>
    /// 面ベクトル配列
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public List<Vector3D> VectorOfPlane = [];

    /// <summary>
    /// 逆格子点ベクトル (kinematical)
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Vector3D[] VectorOfG = [];

    /// <summary>
    /// 逆格子点ベクトル (kinematical)のパラレルクエリ。VectorOfGを初期化すると、これもセットで初期化される。
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public ParallelQuery<Vector3D> VectorOfG_P;


    /// <summary>
    /// 菊池線ベクトル
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public List<Vector3D> VectorOfG_KikuchiLine = [];

    /// <summary>
    /// 極ベクトル
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public List<Vector3D> VectorOfPole = [];
    #endregion

    #region ベーテ法
    /// <summary>
    /// ベーテ法による動力学回折を提供するフィールド
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public BetheMethod Bethe;
    #endregion

    #region 格子定数関係
    /// <summary>
    /// 格子定数
    /// </summary>
    public double A, B, C, Alpha, Beta, Gamma;

    /// <summary>
    /// 格子定数の誤差
    /// </summary>
    public double A_err, B_err, C_err, Alpha_err, Beta_err, Gamma_err;  //格子定数の誤差
    public (double A, double B, double C, double Alpha, double Beta, double Gamma) CellValue
    {
        get => (A, B, C, Alpha, Beta, Gamma);
        set { A = value.A; B = value.B; C = value.C; Alpha = value.Alpha; Beta = value.Beta; Gamma = value.Gamma; }
    }

    [NonSerialized]
    [XmlIgnore]
    public double InitialA, InitialB, InitialC, InitialAlpha, InitialBeta, InitialGamma;    //格子定数

    [NonSerialized]
    [XmlIgnore]
    public double InitialA_err, InitialB_err, InitialC_err, InitialAlpha_err, InitialBeta_err, InitialGamma_err;    //格子定数の誤差

    [NonSerialized]
    [XmlIgnore]
    public double CellVolumeSquare;

    [NonSerialized]
    [XmlIgnore]
    private double sigma11, sigma22, sigma33, sigma23, sigma31, sigma12;

    /// <summary>
    /// 単位格子体積(nm^3)
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public double Volume, Volume_err;

    /// <summary>
    /// a軸ベクトル
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Vector3DBase A_Axis;

    /// <summary>
    /// b軸ベクトル
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Vector3DBase B_Axis;

    /// <summary>
    /// c軸ベクトル
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Vector3DBase C_Axis;

    /// <summary>
    /// a*軸ベクトル
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Vector3DBase A_Star;

    /// <summary>
    /// b*軸ベクトル
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Vector3DBase B_Star;

    /// <summary>
    /// c*軸ベクトル
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Vector3DBase C_Star;

    /// <summary>
    /// 逆格子行列 (1行目にa*, 2行目にb*, 3行目にｃ*)
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Matrix3D MatrixInverse = new();

    /// <summary>
    /// 逆格子行列の転置行列
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Matrix3D MatrixInverseTransposed = new();

    /// <summary>
    /// 実格子行列 (1列目にa, 2列目にb, 3列目にｃ)
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Matrix3D MatrixReal = new();
    #endregion

    #region 対称性関係
    /// <summary>
    /// 空間群の通し番号
    /// </summary>
    public int SymmetrySeriesNumber = 0;

    /// <summary>
    /// 対称性
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Symmetry Symmetry;
    #endregion

    #region 回転状態
    public Matrix3D RotationMatrix = new();
    #endregion

    #region 結晶の名称、色、文献情報
    /// <summary>
    /// 結晶の名称
    /// </summary>
    public string Name = "";
    /// <summary>
    /// 結晶の色
    /// </summary>
    public int Argb;

    public string Note = "";

    public string PublAuthorName = "";

    public string PublSectionTitle = "";

    public string Journal;//細かく分けられない場合これをつかう。分けられるときは以下を使う。
    public string JournalName = "";
    public string JournalVolume = "";
    public string JournalIssue = "";
    public string JournalYear = "";
    public string JournalPageFirst = "";
    public string JournalPageLast = "";

    public (string Summary, string Name, string Volume, string JIssue, string Year, string PageFirst, string PageLast)
        JournalInformation => (Journal, JournalName, JournalVolume, JournalIssue, JournalYear, JournalPageFirst, JournalPageLast);
    #endregion

    #region 密度、化学組成関連
    /// <summary>
    /// 密度 (g/cc)
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public double Density, Density_err;//密度

    [NonSerialized]
    [XmlIgnore]
    public double WeightPerFormula;

    [NonSerialized]
    [XmlIgnore]
    public string ChemicalFormulaSum = "";//計算可能な場合は。

    [NonSerialized]
    [XmlIgnore]
    public string ChemicalFormulaStructural = "";

    [NonSerialized]
    [XmlIgnore]
    public int ChemicalFormulaZ;

    /// <summary>
    /// 元素名配列
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public string[] ElementName;

    /// <summary>
    /// 元素番号配列
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public double[] ElementNum;
    #endregion

    #region 原子情報
    /// <summary>
    /// 原子の情報を取り扱うAtomsクラスの配列
    /// </summary>
    public Atoms[] Atoms = [];

    /// <summary>
    /// 原子の情報を取り扱うAtomsクラスのParallelQuery
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public ParallelQuery<Atoms> AtomsP;
    #endregion

    #region 結合情報、格子面、描画境界の情報　StructureViewerで使用
    /// <summary>
    /// 結合の情報を取り扱うBondクラスの配列
    /// </summary>
    public Bonds[] Bonds = [];

    public Bound[] Bounds;

    public LatticePlane[] LatticePlanes;
    #endregion

    #region 歪みテンソル、応力テンソル、弾性定数

    /// <summary>
    /// 格子歪みテンソル
    /// </summary>
    public Matrix3D Strain = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// 応力テンソル
    /// </summary>
    public Matrix3D Stress = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// ヒル定数
    /// </summary>
    public double HillCoefficient = 0;

    /// <summary>
    /// 弾性定数 (保存用)
    /// </summary>
    public double[] ElasticStiffnessArray = new double[21];

    /// <summary>
    /// 弾性定数マトリックス
    /// </summary>
    [XmlIgnore]
    public double[,] ElasticStiffness
    {
        set
        {
            ElasticStiffnessArray = [
                 value[0, 0],value[0, 1], value[0, 2] ,value[0, 3] , value[0, 4] ,value[0, 5] ,
                 value[1, 1], value[1, 2] ,value[1, 3] , value[1, 4] ,value[1, 5] ,
                 value[2, 2] ,value[2, 3] , value[2, 4] ,value[2, 5] ,
                 value[3, 3] , value[3, 4] ,value[3, 5] ,
                 value[4, 4] ,value[4, 5] ,
                  value[5, 5]
                ];
        }
        get
        {
            var mtx = new double[6, 6];
            mtx[0, 0] = ElasticStiffnessArray[0];
            mtx[0, 1] = mtx[1, 0] = ElasticStiffnessArray[1];
            mtx[0, 2] = mtx[2, 0] = ElasticStiffnessArray[2];
            mtx[0, 3] = mtx[3, 0] = ElasticStiffnessArray[3];
            mtx[0, 4] = mtx[4, 0] = ElasticStiffnessArray[4];
            mtx[0, 5] = mtx[5, 0] = ElasticStiffnessArray[5];
            mtx[1, 1] = ElasticStiffnessArray[6];
            mtx[1, 2] = mtx[2, 1] = ElasticStiffnessArray[7];
            mtx[1, 3] = mtx[3, 1] = ElasticStiffnessArray[8];
            mtx[1, 4] = mtx[4, 1] = ElasticStiffnessArray[9];
            mtx[1, 5] = mtx[5, 1] = ElasticStiffnessArray[10];
            mtx[2, 2] = ElasticStiffnessArray[11];
            mtx[2, 3] = mtx[3, 2] = ElasticStiffnessArray[12];
            mtx[2, 4] = mtx[4, 2] = ElasticStiffnessArray[13];
            mtx[2, 5] = mtx[5, 2] = ElasticStiffnessArray[14];
            mtx[3, 3] = ElasticStiffnessArray[15];
            mtx[3, 4] = mtx[4, 3] = ElasticStiffnessArray[16];
            mtx[3, 5] = mtx[5, 3] = ElasticStiffnessArray[17];
            mtx[4, 4] = ElasticStiffnessArray[18];
            mtx[4, 5] = mtx[5, 4] = ElasticStiffnessArray[19];
            mtx[5, 5] = ElasticStiffnessArray[20];
            return mtx;
        }
    }
    #endregion

    #region EOS関連
    /// <summary>
    /// EOSのパラメータ
    /// </summary>
    public EOS EOSCondition = new();

    /// <summary>
    /// EOSを利用するかどうか
    /// </summary>
    public bool DoesUseEOS = false;
    #endregion

    #region 多結晶体に関する情報 
    /// <summary>
    /// 方位とサイズを保持したCrystalliteクラス配列 (多結晶体計算時に用いる)
    /// </summary>
    [NonSerialized]
    [XmlIgnore]
    public Crystallite Crystallites;

    public double AngleResolution = 2;
    public int SubDivision = 4;

    /// <summary>
    /// 結晶子のサイズ (単位:nm)
    /// </summary>
    public double GrainSize = 100;

    public int id = 0;
    #endregion

    #endregion プロパティ、フィールド

    #region コンストラクタ

    public Crystal()
    {
        Symmetry = SymmetryStatic.Symmetries[0];
        Plane = [];
        Atoms = [];
        ElasticStiffnessArray = new double[21];
        Bethe = new BetheMethod(this);
        A = B = C = Alpha = Beta = Gamma = 0;
        Name = "";
        Argb = Color.Black.ToArgb();
    }

    public Crystal(
        (double A, double B, double C, double Alpha, double Beta, double Gamma) cell,
        int symmetrySeriesNumber,
        string name,
        Color col)
        : this()
    {
        A = cell.A; B = cell.B; C = cell.C; Alpha = cell.Alpha; Beta = cell.Beta; Gamma = cell.Gamma;
        Name = name;
        Argb = col.ToArgb();
        SymmetrySeriesNumber = symmetrySeriesNumber;
        Symmetry = SymmetryStatic.Symmetries[SymmetrySeriesNumber];

        SetAxis();
    }
    public Crystal(
        (double A, double B, double C, double Alpha, double Beta, double Gamma) cell,
        (double A, double B, double C, double Alpha, double Beta, double Gamma)? err,
        int symmetrySeriesNumber,
        string name,
        Color col)
        : this(cell, symmetrySeriesNumber, name, col)
    {
        if (err != null)
        {
            A_err = err.Value.A; B_err = err.Value.B; C_err = err.Value.C; Alpha_err = err.Value.Alpha; Beta_err = err.Value.Beta; Gamma_err = err.Value.Gamma;
        }
    }

    public Crystal(
      (double A, double B, double C, double Alpha, double Beta, double Gamma) cell,
      (double A, double B, double C, double Alpha, double Beta, double Gamma)? err,
      int symmetrySeriesNumber,
      string name,
      Color col,
      Matrix3D rot,
      Atoms[] atoms,
      (string Note, string Authors, string Journal, string Title) reference,
      Bonds[] bonds)
      : this(cell, err, symmetrySeriesNumber, name, col)
    {
        Atoms = atoms;
        for (int i = 0; i < atoms.Length; i++)
            Atoms[i].ResetSymmetry(symmetrySeriesNumber);

        AtomsP = Atoms.AsParallel();

        Note = reference.Note; PublAuthorName = reference.Authors; Journal = reference.Journal; PublSectionTitle = reference.Title;
        RotationMatrix = new Matrix3D(rot);
        GetFormulaAndDensity();
        Bonds = bonds;
    }


    public Crystal(
      (double A, double B, double C, double Alpha, double Beta, double Gamma) cell,
      (double A, double B, double C, double Alpha, double Beta, double Gamma)? err,
      int symmetrySeriesNumber,
      string name,
      Color col,
      Matrix3D rot,
      Atoms[] atoms,
      (string Note, string Authors, string Journal, string Title) reference,
      Bonds[] bonds,
      Bound[] bounds,
      LatticePlane[] latticePlane,
      EOS eos)
      : this(cell, err, symmetrySeriesNumber, name, col, rot, atoms, reference, bonds)
    {

        Bounds = bounds;
        for (int i = 0; i < Bounds.Length; i++)
            Bounds[i].Reset(this);
        LatticePlanes = latticePlane;
        for (int i = 0; i < LatticePlanes.Length; i++)
            LatticePlanes[i].Reset(this);

        EOSCondition = eos;
    }
    public Crystal(Crystal cry)
    {
        A = cry.A; B = cry.B; C = cry.C; Alpha = cry.Alpha; Beta = cry.Beta; Gamma = cry.Gamma;
        A_err = cry.A_err; B_err = cry.B_err; C_err = cry.C_err; Alpha_err = cry.Alpha_err; Beta_err = cry.Beta_err; Gamma_err = cry.Gamma_err;
        Name = cry.Name; Note = cry.Note; /*color = col;*/ Argb = cry.Argb;
        SetAxis();

        SymmetrySeriesNumber = cry.SymmetrySeriesNumber;
        Symmetry = SymmetryStatic.Symmetries[SymmetrySeriesNumber];

        Atoms = cry.Atoms;

        PublAuthorName = cry.PublAuthorName;
        Journal = cry.Journal;
        PublSectionTitle = cry.PublSectionTitle;

        GetFormulaAndDensity();

        Bonds = cry.Bonds;

        LatticePlanes = cry.LatticePlanes;
        Bounds = cry.Bounds;

        ElasticStiffnessArray = cry.ElasticStiffnessArray;
    }

    #endregion コンストラクタ

    #region Crystal2クラスに変換
    /// <summary>
    /// Crystal2クラスに変換します。
    /// </summary>
    public Crystal2 ToCrystal2() => Crystal2.FromCrystal(this);
    #endregion

    #region 各軸のベクトルや補助定数(sigmaなど)を設定
    /// <summary>
    /// 格子定数から、各軸のベクトルや補助定数(sigmaなど)を設定する
    /// </summary>
    public void SetAxis()
    {
        #region まず、対称性に即した格子定数になるように強制する
        switch (Symmetry.CrystalSystemStr)
        {
            case "monoclinic":
                switch (Symmetry.MainAxis)
                {
                    case "a":
                        Beta = Gamma = Math.PI / 2;
                        Beta_err = Gamma_err = 0;
                        break;

                    case "b":
                        Alpha = Gamma = Math.PI / 2;
                        Alpha_err = Gamma_err = 0;

                        break;

                    case "c":
                        Alpha = Beta = Math.PI / 2;
                        Alpha_err = Beta_err = 0;
                        break;
                }
                break;

            case "orthorhombic":
                Alpha = Beta = Gamma = Math.PI / 2;
                Alpha_err = Beta_err = Gamma_err = 0;
                break;

            case "tetragonal":
                B = A;
                B_err = A_err;
                Alpha = Beta = Gamma = Math.PI / 2;
                Alpha_err = Beta_err = Gamma_err = 0;
                break;

            case "trigonal":
                switch (Symmetry.SpaceGroupHMStr.Contains("Rho") && Symmetry.SpaceGroupHMStr.Contains('R'))
                {
                    case false:
                        B = A;
                        B_err = A_err;
                        Alpha = Beta = Math.PI / 2;
                        Gamma = Math.PI * 2.0 / 3.0;
                        Alpha_err = Beta_err = Gamma_err = 0;
                        break;

                    case true:
                        C = B = A;
                        C_err = B_err = A_err;

                        Alpha = Beta = Gamma;
                        Alpha_err = Beta_err = Gamma_err;
                        break;
                }
                break;

            case "hexagonal":
                B = A;
                B_err = A_err;
                Alpha = Beta = Math.PI / 2;
                Gamma = Math.PI * 2.0 / 3.0;
                Alpha_err = Beta_err = Gamma_err = 0;
                break;

            case "cubic":
                C = B = A;
                C_err = B_err = A_err;
                Alpha = Beta = Gamma = Math.PI / 2;
                Alpha_err = Beta_err = Gamma_err = 0;
                break;
        }
        #endregion

        double SinAlpha = Math.Sin(Alpha), SinBeta = Math.Sin(Beta), SinGamma = Math.Sin(Gamma);
        double CosAlpha = Math.Cos(Alpha), CosBeta = Math.Cos(Beta), CosGamma = Math.Cos(Gamma);
        double a2 = A * A; double b2 = B * B; var c2 = C * C;

        C_Axis = new Vector3DBase(0, 0, C);
        B_Axis = new Vector3DBase(0, B * SinAlpha, B * CosAlpha);
        A_Axis = new Vector3DBase(
        A * Math.Sqrt(1 - CosBeta * CosBeta - (CosGamma - CosAlpha * CosBeta) * (CosGamma - CosAlpha * CosBeta) / SinAlpha / SinAlpha),
        A * (CosGamma - CosAlpha * CosBeta) / SinAlpha,
        A * CosBeta);

        MatrixReal = new Matrix3D(A_Axis, B_Axis, C_Axis);
        MatrixInverse = Matrix3D.Inverse(MatrixReal);
        MatrixInverseTransposed = MatrixInverse.Transpose();

        A_Star = new Vector3DBase(MatrixInverse.E11, MatrixInverse.E12, MatrixInverse.E13);
        B_Star = new Vector3DBase(MatrixInverse.E21, MatrixInverse.E22, MatrixInverse.E23);
        C_Star = new Vector3DBase(MatrixInverse.E31, MatrixInverse.E32, MatrixInverse.E33);

        sigma11 = b2 * c2 * SinAlpha * SinAlpha;
        sigma22 = c2 * a2 * SinBeta * SinBeta;
        sigma33 = a2 * b2 * SinGamma * SinGamma;
        sigma23 = a2 * B * C * (CosBeta * CosGamma - CosAlpha);
        sigma31 = A * b2 * C * (CosGamma * CosAlpha - CosBeta);
        sigma12 = A * B * c2 * (CosAlpha * CosBeta - CosGamma);
        CellVolumeSquare = a2 * b2 * c2 * (1 - CosAlpha * CosAlpha - CosBeta * CosBeta - CosGamma * CosGamma + 2 * CosAlpha * CosBeta * CosGamma);
        Volume = Math.Sqrt(CellVolumeSquare);
    }
    #endregion

    #region 結晶幾何学関連

    /// <summary>
    /// (h,k,l)面の面間隔を計算します
    /// </summary>
    /// <param name="h"></param>
    /// <param name="k"></param>
    /// <param name="l"></param>
    /// <returns></returns>
    public double GetLengthPlane(int h, int k, int l)
    {
        if ((h == 0 && k == 0 && l == 0) || A * B * C == 0 || CellVolumeSquare <= 0) return 0;
        return Symmetry.CrystalSystemStr switch//場合分けしたほうが早いかな？
        {
            "cubic" => A / Math.Sqrt(h * h + k * k + l * l),
            "tetragoanl" => 1 / Math.Sqrt((h * h + k * k) / A / A + l * l / C / C),
            "orthorhombic" => 1 / Math.Sqrt(h * h / A / A + k * k / B / B + l * l / C / C),
            _ => Math.Sqrt(1.0 / (h * h * sigma11 + k * k * sigma22 + l * l * sigma33 + 2 * k * l * sigma23 + 2 * l * h * sigma31 + 2 * h * k * sigma12) * CellVolumeSquare),
        };
    }

    /// <summary>
    /// (h1 k1 l1)面と(h2 l2 k2)面のなす角を計算します
    /// </summary>
    /// <param name="h1"></param>
    /// <param name="k1"></param>
    /// <param name="l1"></param>
    /// <param name="h2"></param>
    /// <param name="k2"></param>
    /// <param name="l2"></param>
    /// <returns></returns>
    public double GetAnglePlanes(int h1, int k1, int l1, int h2, int k2, int l2)
    {
        if (A * B * C == 0 || (h1 == 0 && k1 == 0 && l1 == 0) || (h2 == 0 && k2 == 0 && l2 == 0) || (h1 == h2 && k1 == k2 && l1 == l2) || CellVolumeSquare <= 0) return 0;
        double temp = GetLengthPlane(h1, k1, l1) * GetLengthPlane(h2, k2, l2) / CellVolumeSquare * (h1 * h2 * sigma11 + k1 * k2 * sigma22 + l1 * l2 * sigma33 + (k1 * l2 + l1 * k2) * sigma23 + (l1 * h2 + h1 * l2) * sigma31 + (h1 * k2 + k1 * h2) * sigma12);
        if (temp >= 1 || temp <= -1) return 0;
        return Math.Acos(temp);
    }

    /// <summary>
    /// 軸[pqr]の周期の長さ
    /// </summary>
    /// <param name="u"></param>
    /// <param name="v"></param>
    /// <param name="w"></param>
    /// <returns></returns>
    public double GetLengthAxis(int u, int v, int w)
    {
        if (A * B * C == 0 || (u == 0 && v == 0 && w == 0)) return 0;
        return Math.Sqrt(u * u * A * A + v * v * B * B + w * w * C * C + 2 * v * w * B * C * Math.Cos(Alpha) + 2 * w * u * C * A * Math.Cos(Beta) + 2 * u * v * A * B * Math.Cos(Gamma));
    }

    /// <summary>
    /// 二軸のなす角
    /// </summary>
    /// <param name="u1"></param>
    /// <param name="v1"></param>
    /// <param name="w1"></param>
    /// <param name="u2"></param>
    /// <param name="v2"></param>
    /// <param name="w2"></param>
    /// <returns></returns>
    public double GetAngleAxes(int u1, int v1, int w1, int u2, int v2, int w2)
    {
        if (A * B * C == 0 || (u1 == 0 && v1 == 0 && w1 == 0) || (u2 == 0 && v2 == 0 && w2 == 0) || (u1 == u2 && v1 == v2 && w1 == w2)) return 0;
        return Math.Acos(1.0 / GetLengthAxis(u1, v1, w1) / GetLengthAxis(u2, v2, w2) * (u1 * u2 * A * A + v1 * v2 * B * B + w1 * w2 * C * C + (v1 * w2 + w1 * v2) * B * C * Math.Cos(Alpha) + (w1 * u2 + u1 * w2) * C * A * Math.Cos(Beta) + (u1 * v2 + v1 * u2) * A * B * Math.Cos(Gamma)));
    }

    /// <summary>
    /// 面の垂線と軸のなす角
    /// </summary>
    /// <param name="h"></param>
    /// <param name="k"></param>
    /// <param name="l"></param>
    /// <param name="u"></param>
    /// <param name="v"></param>
    /// <param name="w"></param>
    /// <returns></returns>
    public double GetAnglePlaneAxis(int h, int k, int l, int u, int v, int w)
    {
        if (A * B * C == 0 || (h == 0 && k == 0 && l == 0) || (u == 0 && v == 0 && w == 0)) return 0;
        return Math.Acos(GetLengthPlane(h, k, l) / GetLengthAxis(u, v, w) * (h * u + k * v + l * w));
    }

    /// <summary>
    /// 2面から成る晶帯軸
    /// </summary>
    /// <param name="h1"></param>
    /// <param name="k1"></param>
    /// <param name="l1"></param>
    /// <param name="h2"></param>
    /// <param name="k2"></param>
    /// <param name="l2"></param>
    /// <returns></returns>
    public static string GetZoneAxis(int h1, int k1, int l1, int h2, int k2, int l2)
    {
        int u, v, w, z;
        u = l1 * k2 - k1 * l2; v = h1 * l2 - l1 * h2; w = k1 * h2 - h1 * k2;
        for (z = 2; z <= Math.Abs(u) || z <= Math.Abs(v) || z <= Math.Abs(w); z++)
            if ((u % z == 0) && (v % z == 0) && (w % z == 0))
            {
                u /= z; v /= z; w /= z; z = 1;
            }
        return $" {u} {v} {w}";
    }

    /// <summary>
    /// a,b,c軸ベクトルからhkl面の方向ベクトル計算
    /// </summary>
    /// <param name="h"></param>
    /// <param name="k"></param>
    /// <param name="l"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Vector3D CalcHklVector(int h, int k, int l, Vector3D a, Vector3D b, Vector3D c)
        => new(
            -h * (b.Z * c.Y - b.Y * c.Z) - k * (c.Z * a.Y - c.Y * a.Z) - l * (a.Z * b.Y - a.Y * b.Z),
            -h * (b.X * c.Z - b.Z * c.X) - k * (c.X * a.Z - c.Z * a.X) - l * (a.X * b.Z - a.Z * b.X),
            -h * (b.Y * c.X - b.X * c.Y) - k * (c.Y * a.X - c.X * a.Y) - l * (a.Y * b.X - a.X * b.Y)
            );

    /// <summary>
    /// hkl面の方向ベクトル計算
    /// </summary>
    /// <param name="h"></param>
    /// <param name="k"></param>
    /// <param name="l"></param>
    /// <returns></returns>
    public Vector3D CalcHklVector(int h, int k, int l)
        => new(
            -h * (B_Axis.Z * C_Axis.Y - B_Axis.Y * C_Axis.Z) - k * (C_Axis.Z * A_Axis.Y - C_Axis.Y * A_Axis.Z) - l * (A_Axis.Z * B_Axis.Y - A_Axis.Y * B_Axis.Z),
            -h * (B_Axis.X * C_Axis.Z - B_Axis.Z * C_Axis.X) - k * (C_Axis.X * A_Axis.Z - C_Axis.Z * A_Axis.X) - l * (A_Axis.X * B_Axis.Z - A_Axis.Z * B_Axis.X),
            -h * (B_Axis.Y * C_Axis.X - B_Axis.X * C_Axis.Y) - k * (C_Axis.Y * A_Axis.X - C_Axis.X * A_Axis.Y) - l * (A_Axis.Y * B_Axis.X - A_Axis.X * B_Axis.Y)
            );

    /// <summary>
    /// 既約かどうか判定
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static bool CheckIrreducible(int a, int b, int c)
    {
        for (int n = 2; n <= new[] { Math.Abs(a), Math.Abs(b), Math.Abs(c) }.Max(); n++)
            if (a % n == 0 && b % n == 0 && c % n == 0)
                return false;
        return true;
    }

    /// <summary>
    /// 二組の(hkl)が既約かどうかを判定
    /// </summary>
    /// <param name="index1"></param>
    /// <param name="index2"></param>
    /// <returns></returns>
    public static bool CheckIrreducible((int h, int k, int l) index1, (int h, int k, int l) index2)
    {
        if (index1.h == index2.h && index1.k == index2.k && index1.l == index2.l)
            return false;

        int coeff1 = 0, coeff2 = 0;
        if (index1.h != 0 && index2.h != 0)
        {
            coeff1 = index1.h / index2.h;
            coeff2 = index2.h / index1.h;
        }
        else if (index1.k != 0 && index2.k != 0)
        {
            coeff1 = index1.k / index2.k;
            coeff2 = index2.k / index1.k;
        }
        else if (index1.l != 0 && index2.l != 0)
        {
            coeff1 = index1.l / index2.l;
            coeff2 = index2.l / index1.l;
        }

        if (Math.Abs(coeff1) > 1)
            return !(index2.h * coeff1 == index1.h && index2.k * coeff1 == index1.k && index2.l * coeff1 == index1.l);
        else if (Math.Abs(coeff2) > 1)
            return !(index1.h * coeff2 == index2.h && index1.k * coeff2 == index2.k && index1.l * coeff2 == index2.l);
        else
            return true;
    }


    #endregion 結晶幾何学関連

    #region 軸ベクトルの計算

    /// <summary>
    ///　引数で指定された指数の軸ベクトルを計算し、VectorOfAxisに格納
    /// </summary>
    /// <param name="indices"></param>
    public void SetVectorOfAxis((int U, int V, int W)[] indices)
    {
        if (A_Axis == null) return;
        VectorOfAxis = [];
        foreach (var (U, V, W) in indices)
            VectorOfAxis.Add(new Vector3D(U * A_Axis + V * B_Axis + W * C_Axis) { Text = $"[{U}{V}{W}]" });
    }

    /// <summary>
    /// 引数で指定されたuMax,vMax,wMaxの範囲で軸ベクトルを計算し、VectorOfAxisに格納
    /// </summary>
    /// <param name="uMax"></param>
    /// <param name="vMax"></param>
    /// <param name="wMax"></param>
    public void SetVectorOfAxis(int uMax, int vMax, int wMax)
    {
        if (A_Axis == null) return;
        VectorOfAxis = [];

        VectorOfAxis.Add(new Vector3D(A_Axis) { Text = "[100]" });
        VectorOfAxis.Add(new Vector3D(B_Axis) { Text = "[010]" });
        VectorOfAxis.Add(new Vector3D(C_Axis) { Text = "[001]" });
        VectorOfAxis.Add(new Vector3D(-A_Axis) { Text = "[-100]" });
        VectorOfAxis.Add(new Vector3D(-B_Axis) { Text = "[0-10]" });
        VectorOfAxis.Add(new Vector3D(-C_Axis) { Text = "[00-1]" });

        for (int u = -uMax; u <= uMax; u++)
            for (int v = -vMax; v <= vMax; v++)
                for (int w = -wMax; w <= wMax; w++)
                    if (CheckIrreducible(u, v, w) && !(u * v == 0 && v * w == 0 && w * u == 0))
                        VectorOfAxis.Add(new Vector3D(u * A_Axis + v * B_Axis + w * C_Axis) { Text = $"[{u}{v}{w}]" });
    }

    #endregion 軸ベクトルの計算

    #region 面ベクトルの計算

    /// <summary>
    /// 引数で指定された指数の面ベクトルを計算し、VectorOfPlaneに格納
    /// </summary>
    /// <param name="indices"></param>
    public void SetVectorOfPlane((int h, int k, int l)[] indices, WaveSource waveSource)
    {
        VectorOfPlane = [];
        foreach (var (h, k, l) in indices)
        {
            var vec = new Vector3D(h * A_Star + k * B_Star + l * C_Star);
            vec.F = GetStructureFactor(waveSource, Atoms, (h, k, l), vec.Length2 / 4.0);
            vec.RawIntensity = vec.F.MagnitudeSquared();
            vec.Text = $"({h}{k}{l})";
            vec.Index = (h, k, l);
            VectorOfPlane.Add(vec);
        }

        if (VectorOfPlane.Count > 0)
        {
            var max = VectorOfPlane.Max(v => v.RawIntensity);
            if (max > 0)
                VectorOfPlane.ForEach(v => v.RelativeIntensity = v.RawIntensity / max);
        }
    }

    /// <summary>
    /// 引数で指定されたhMax,kMax,lMaxの範囲で軸ベクトルを計算し、VectorOfAxisに格納
    /// </summary>
    /// <param name="hMax"></param>
    /// <param name="kMax"></param>
    /// <param name="lMax"></param>
    public void SetVectorOfPlane(int hMax, int kMax, int lMax, WaveSource waveSource)
    {
        var indices = new List<(int h, int k, int l)> { (1, 0, 0), (0, 1, 0), (0, 0, 1), (-1, 0, 0), (0, -1, 0), (0, 0, -1) };
        for (int h = -hMax; h <= hMax; h++)
            for (int k = -kMax; k <= kMax; k++)
                for (int l = -lMax; l <= lMax; l++)
                    if (CheckIrreducible(h, k, l) && !(h * k == 0 && k * l == 0 && l * h == 0))
                        indices.Add((h, k, l));

        SetVectorOfPlane([.. indices], waveSource);
    }

    /// <summary>
    /// 面間隔d_limit以上の面を検索、ソートする。
    /// </summary>
    /// <param name="dMax">この値以上の面を検索する</param>
    /// <param name="dMin">この値以下の面を検索する</param>
    /// <param name="excludeEquivalentPlane">trueのときは結晶学的に等価な面を排除する</param>
    /// <param name="excludeForbiddenPlane">trueのときは消滅則に引っかかる面を排除する</param>
    /// <param name="excludeSameDistance">trueのときは結晶学的には非等価だが、面間隔がまったく同じ面を排除する</param>
    /// <param name="combineAdjacentPeak">trueのときは、近いピークを結合する</param>
    /// <param name="horizontalAxis">横軸の単位を指定する</param>
    /// <param name="horizontalThreshold">指定された横軸単位における差がこの閾値以下の面どうしを統合する</param>
    /// <param name="horizontalParameter">横軸が角度の時は入射線の波長を、エネルギーの時は取り出し角を指定する</param>
    public void SetPlanes(double dMax, double dMin, bool excludeEquivalentPlane, bool excludeForbiddenPlane, bool excludeSameDistance, bool combineAdjacentPeak,
        HorizontalAxis horizontalAxis, double horizontalThreshold, double horizontalParameter, int _maxNum = 8000)
    {
        if (A_Star == null) SetAxis();
        if (double.IsNaN(MatrixInverse.Column1.X)) return;

        double aX = A_Star.X, aY = A_Star.Y, aZ = A_Star.Z;
        double bX = B_Star.X, bY = B_Star.Y, bZ = B_Star.Z;
        double cX = C_Star.X, cY = C_Star.Y, cZ = C_Star.Z;

        var gMax = 1 / dMin;
        (int h, int k, int l)[] directions = [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)];

        var shift = directions.Select(dir => (MatrixInverse * dir).Length).Max();

        var maxNum = _maxNum;
        var outer = new List<(int H, int K, int L, double len)>() { (0, 0, 0, 0) };
        var gHash = new HashSet<(int H, int K, int L)>((int)(maxNum * 1.5)) { (0, 0, 0) }; //全てのhklを検索するため、composeを使えないことに注意
        var minG = 0.0;
        var listPlane = new List<Plane>((int)(maxNum * 1.5));

        while (listPlane.Count < maxNum && (minG = outer.Min(o => o.len)) < gMax)
        {
            var end = outer.FindLastIndex(o => o.len - minG < shift * 2);
            foreach (var (h1, k1, l1, _) in CollectionsMarshal.AsSpan(outer)[..(end + 1)])
            {
                foreach ((int h2, int k2, int l2) in directions)
                {
                    int h = h1 + h2, k = k1 + k2, l = l1 + l2;
                    if (gHash.Add((h, k, l)))
                    {
                        double x = h * aX + k * bX + l * cX, y = h * aY + k * bY + l * cY, z = h * aZ + k * bZ + l * cZ;
                        var len = Math.Sqrt(x * x + y * y + z * z);
                        outer.Add((h, k, l, len));
                        if (len < gMax && len > 1 / dMax)
                        {
                            var root = SymmetryStatic.IsRootIndex((h, k, l), Symmetry, out int multi);
                            var extinction = Symmetry.CheckExtinctionRule(h, k, l);
                            if ((!excludeEquivalentPlane || root) && (!excludeForbiddenPlane || extinction.Length == 0))
                            {
                                listPlane.Add(new Plane
                                {
                                    IsRootIndex = root,
                                    h = h,
                                    k = k,
                                    l = l,
                                    d = 1 / len,
                                    strHKL = $"{h} {k} {l}",
                                    Multi = [multi],
                                });
                            }
                        }
                    }
                }
            }
            outer.RemoveRange(0, end + 1);
            outer.Sort((e1, e2) => e1.len.CompareTo(e2.len));
        }

        #region お蔵入り
        //if (dMin < (A + B + C) / 3 / 30)
        //    dMin = (A + B + C) / 3 / 30;
        //var hMax = (int)(A / dMin);
        //var kMax = (int)(B / dMin);
        //var lMax = (int)(C / dMin);



        //for (int h = -hMax; h <= hMax; h++)//hは0からはじめる
        //    for (int k = -kMax; k <= kMax; k++)
        //        for (int l = -lMax; l <= lMax; l++)
        //            if ((d = GetLengthPlane(h, k, l)) > dMin && d < dMax)
        //                if (!excludeEquivalentPlane || SymmetryStatic.IsRootIndex((h, k, l), Symmetry, ref multi))
        //                {
        //                    var temp = new Plane { IsRootIndex = SymmetryStatic.IsRootIndex((h, k, l), Symmetry, ref multi) };
        //                    if (!excludeForbiddenPlane || (temp.strCondition = Symmetry.CheckExtinctionRule(h, k, l)).Length == 0)
        //                    {
        //                        temp.Multi[0] = multi;
        //                        temp.h = h; temp.k = k; temp.l = l; temp.d = d;
        //                        temp.strHKL = $"{h} {k} {l}";
        //                        temp.IsRootIndex = true;
        //                        listPlane.Add(temp);
        //                    }
        //                }
        #endregion

        listPlane.Sort();

        if (excludeSameDistance)//全くおなじ結晶面面間隔をもつもの(511と333とか)を排除する
        {
            int i = 0;
            while (i < listPlane.Count - 1)
            {
                if (listPlane[i].d == listPlane[i + 1].d)
                    listPlane.RemoveAt(i + 1);
                else
                    i++;
            }
        }

        if (combineAdjacentPeak)//d値の近いピークを結合する
        {
            if (horizontalAxis == HorizontalAxis.Angle && horizontalThreshold > 0 && horizontalParameter > 0) //閾値が角度の場合
            {
                double minDif = double.PositiveInfinity;
                do
                {
                    minDif = double.PositiveInfinity;
                    int k = 0;
                    for (int i = 0; i < listPlane.Count - 1; i++)
                        if (minDif > Math.Abs(Math.Asin(horizontalParameter / listPlane[i].d / 2.0) * 2 - Math.Asin(horizontalParameter / listPlane[i + 1].d / 2.0) * 2))
                        {
                            minDif = Math.Abs(Math.Asin(horizontalParameter / listPlane[i].d / 2.0) * 2 - Math.Asin(horizontalParameter / listPlane[i + 1].d / 2.0) * 2);
                            k = i;
                        }
                    if (minDif < horizontalThreshold)
                    {
                        listPlane[k].d = (listPlane[k].d + listPlane[k + 1].d) / 2.0;
                        listPlane[k].strHKL += " + " + listPlane[k + 1].strHKL;
                        int[] multiplisity = new int[listPlane[k].Multi.Length + listPlane[k + 1].Multi.Length];
                        listPlane[k].Multi.CopyTo(multiplisity, 0);
                        listPlane[k + 1].Multi.CopyTo(multiplisity, listPlane[k].Multi.Length);
                        listPlane[k].Multi = multiplisity;

                        listPlane.RemoveAt(k + 1);
                    }
                } while (minDif < horizontalThreshold);
            }
            else if (horizontalAxis == HorizontalAxis.EnergyXray && horizontalThreshold > 0 && horizontalParameter > 0)//Energyの場合
            {
                double minDif = double.PositiveInfinity;
                do
                {
                    minDif = double.PositiveInfinity;
                    int k = 0;
                    for (int i = 0; i < listPlane.Count - 1; i++)
                        if (minDif > Math.Abs(UniversalConstants.Convert.DspacingToXrayEnergy(listPlane[i].d, horizontalParameter) - UniversalConstants.Convert.DspacingToXrayEnergy(listPlane[i + 1].d, horizontalParameter)))
                        {
                            minDif = Math.Abs(UniversalConstants.Convert.DspacingToXrayEnergy(listPlane[i].d, horizontalParameter) - UniversalConstants.Convert.DspacingToXrayEnergy(listPlane[i + 1].d, horizontalParameter));
                            k = i;
                        }
                    if (minDif < horizontalThreshold)
                    {
                        listPlane[k].d = (listPlane[k].d + listPlane[k + 1].d) / 2.0;
                        listPlane[k].strHKL += " + " + listPlane[k + 1].strHKL;
                        int[] multiplisity = new int[listPlane[k].Multi.Length + listPlane[k + 1].Multi.Length];
                        listPlane[k].Multi.CopyTo(multiplisity, 0);
                        listPlane[k + 1].Multi.CopyTo(multiplisity, listPlane[k].Multi.Length);
                        listPlane[k].Multi = multiplisity;

                        listPlane.RemoveAt(k + 1);
                    }
                } while (minDif < horizontalThreshold);
            }
            else if (horizontalAxis == HorizontalAxis.d && horizontalThreshold >= 0)
            {
                double minDif = double.PositiveInfinity;
                do
                {
                    minDif = double.PositiveInfinity;
                    int k = 0;
                    for (int i = 0; i < listPlane.Count - 1; i++)
                        if (minDif > Math.Abs(listPlane[i].d - listPlane[i + 1].d))
                        {
                            minDif = Math.Abs(listPlane[i].d - listPlane[i + 1].d);
                            k = i;
                        }
                    if (minDif <= horizontalThreshold)
                    {
                        listPlane[k].d = (listPlane[k].d + listPlane[k + 1].d) / 2.0;
                        listPlane[k].strHKL += " + " + listPlane[k + 1].strHKL;
                        int[] multiplisity = new int[listPlane[k].Multi.Length + listPlane[k + 1].Multi.Length];
                        listPlane[k].Multi.CopyTo(multiplisity, 0);
                        listPlane[k + 1].Multi.CopyTo(multiplisity, listPlane[k].Multi.Length);
                        listPlane[k].Multi = multiplisity;

                        listPlane.RemoveAt(k + 1);
                    }
                } while (minDif <= horizontalThreshold);
                ;
            }
        }

        //var temp_plane = listPlane.ToArray();
        for (int n = 0; n < listPlane.Count; n++)
        {
            listPlane[n].F2[0] = -1;
            listPlane[n].IsFittingChecked = false;
            listPlane[n].IsFittingSelected = false;
            listPlane[n].num = n;
            listPlane[n].SerchRange = 0.10;
            listPlane[n].SerchOption = listPlane[n].peakFunction.Option = PeakFunctionForm.PseudoVoigt;
            listPlane[n].Intensity = -1;
        }

        if (Plane != null)
            for (int n = 0; n < Plane.Count && n < listPlane.Count; n++)
            {
                listPlane[n].SerchRange = Plane[n].SerchRange;
                listPlane[n].FWHM = Plane[n].FWHM;
                listPlane[n].SerchOption = Plane[n].SerchOption;
                listPlane[n].IsFittingChecked = Plane[n].IsFittingChecked;
                listPlane[n].simpleParameter = Plane[n].simpleParameter;
                listPlane[n].peakFunction = Plane[n].peakFunction;
                listPlane[n].Intensity = Plane[n].Intensity;
                listPlane[n].observedIntensity = Plane[n].observedIntensity;
            }
        Plane.Clear();
        Plane.AddRange(listPlane);
    }
    #endregion 面ベクトルの計算

    # region plene[]のd値を計算する。計算する面の範囲は変えない
    public void SetPlanes()
    {
        for (int i = 0; i < Plane.Count; i++)
            Plane[i].d = GetLengthPlane(Plane[i].h, Plane[i].k, Plane[i].l);
    }

    #endregion

    #region 逆格子ベクトルの計算

    /// <summary>
    /// h, k, l の指数をIntひとつに変換する。(hkl) & h>0 あるいは (0kl) & k > 0 あるいは (00l) & l > 0 の指数しか取り扱わないことに注意
    /// </summary>
    /// <param name="h"></param>
    /// <param name="k"></param>
    /// <param name="l"></param>
    /// <returns></returns>
    static int composeKey(in int h, in int k, in int l) => ((h > 0) || (h == 0 && k > 0) || (h == 0 && k == 0 && l > 0)) ? ((h + 255) << 20) + ((k + 255) << 10) + l + 255 : -1;
    static (int h, int k, int l) decomposeKey(in int key) => (((key << 2) >> 22) - 255, ((key << 12) >> 22) - 255, ((key << 22) >> 22) - 255);

    private readonly Lock lockObj = new();


    /// <summary>
    /// dMin以上の逆格子ベクトルを計算し、wavesorceに従って、構造因子を計算
    /// </summary>
    /// <param name="dMin"></param>
    /// <param name="wavesource"></param>
    /// <param name="excludeLatticeCondition"></param>
    public void SetVectorOfG(double dMin, WaveSource wavesource, int maxNum = 25000)
        => SetVectorOfG(dMin, double.PositiveInfinity, wavesource, maxNum);

    /// <summary>
    /// dMin以上、dMax以下の範囲で逆格子ベクトルを計算し、wavesorceに従って、構造因子を計算
    /// </summary>
    /// <param name="dMin"></param>
    /// <param name="dMax"></param>
    /// <param name="wavesource"></param>
    public void SetVectorOfG(double dMin, double dMax, WaveSource wavesource, int maxNum = 25000)
    {
        if (double.IsNaN(dMin)) return;

        if (A_Star == null) SetAxis();

        double aX = A_Star.X, aY = A_Star.Y, aZ = A_Star.Z;
        double bX = B_Star.X, bY = B_Star.Y, bZ = B_Star.Z;
        double cX = C_Star.X, cY = C_Star.Y, cZ = C_Star.Z;

        double gMax = 1 / dMin, gMax2 = gMax * gMax;

        #region directionを初期化
        var directions = new[] { (1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) }.ToFrozenSet();

        //if (excludeLatticeCondition)
        //{
        //if (Symmetry.LatticeTypeStr == "F")
        //    directions = new [] { (1, 1, 1), (1, 1, -1), (1, -1, 1), (1, -1, -1), (-1, 1, 1), (-1, 1, -1), (-1, -1, 1), (-1, -1, -1) };
        //else if (Symmetry.LatticeTypeStr == "A")
        //    directions = new [] { (0, 1, 1), (0, 1, -1), (0, -1, 1), (0, -1, -1), (1, 0, 0), (-1, 0, 0) };
        //else if (Symmetry.LatticeTypeStr == "B")
        //    directions = new [] { (1, 0, 1), (1, 0, -1), (-1, 0, 1), (-1, 0, -1), (0, 1, 0), (0, -1, 0) };
        //else if (Symmetry.LatticeTypeStr == "C")
        //    directions = new [] { (1, 1, 0), (1, -1, 0), (-1, 1, 0), (-1, -1, 0), (0, 0, 1), (0, 0, -1) };
        //else if (Symmetry.LatticeTypeStr == "I")
        //    directions = new [] { (1, 1, 0), (1, -1, 0), (-1, 1, 0), (-1, -1, 0), (0, 1, 1), (0, 1, -1), (0, -1, 1), (0, -1, -1), (1, 0, 1), (1, 0, -1), (-1, 0, 1), (-1, 0, -1) };
        //else if (Symmetry.LatticeTypeStr == "R" && Symmetry.SpaceGroupHMsubStr == "H")
        //    directions = new [] { (1, 0, 1), (0, -1, 1), (-1, 1, 1), (-1, 0, -1), (0, 1, -1), (1, -1, -1) };
        //else if (Symmetry.CrystalSystemStr == "trigonal" || Symmetry.CrystalSystemStr == "hexagonal")
        //    directions = new [] { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (1, -1, 0), (-1, 1, 0), (0, 0, 1), (0, 0, -1) };
        //else
        //directions = new[] { (1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) };//(-1, 0, 0)は除いておく
        //}
        //else
        // directions = new[] { (1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) };//(-1, 0, 0)は除いておく

        #endregion

        var shift = directions.Select(dir => (MatrixInverse * dir).Length).Max();

        //var maxNum = 250000;
        var zeroKey = (255 << 20) + (255 << 10) + 255;
        var gHash = new HashSet<int>((int)(maxNum * 1.5)) { zeroKey };
        var gList = new List<(int key, double x, double y, double z, double len)>((int)(maxNum * 1.5)) { (zeroKey, 0, 0, 0, 0) };
        int start = 0, end = 1;
        var outer = CollectionsMarshal.AsSpan(gList)[start..end];
        while (gList.Count < maxNum && outer.Length > 0)
        {
            foreach (var (key1, _, _, _, _) in outer)
            {
                var (h1, k1, l1) = decomposeKey(key1);
                foreach (var (h2, k2, l2) in directions)
                {
                    int h = h1 + h2, k = k1 + k2, l = l1 + l2, key2 = composeKey(h, k, l);
                    if (key2 > 0 && gHash.Add(key2))
                    {
                        double x = h * aX + k * bX + l * cX, y = h * aY + k * bY + l * cY, z = h * aZ + k * bZ + l * cZ, len2 = x * x + y * y + z * z;
                        if (len2 < gMax2)
                            gList.Add((key2, x, y, z, Math.Sqrt(len2)));
                    }
                }
            }
            start += end;
            outer = CollectionsMarshal.AsSpan(gList)[start..];
            outer.Sort((e1, e2) => e1.len.CompareTo(e2.len));
            for (end = 0; end < outer.Length && outer[end].len < outer[0].len + shift * 2; end++)
                ;
            outer = outer[..end];
        }
        gList.RemoveAt(0);// 000スポットを削除
        if (!double.IsInfinity(dMax))
        {
            var i = gList.FindIndex(g => g.len > 1 / dMax);
            if (i > 0)
                gList.RemoveRange(0, i);
        }
        VectorOfG = new Vector3D[gList.Count * 2];
        Parallel.For(0, gList.Count, i =>
        {
            var (key, x, y, z, glen) = gList[i];
            var (h, k, l) = decomposeKey(key);
            var extinction = Symmetry.CheckExtinctionRule(h, k, l);
            VectorOfG[i * 2] = new Vector3D(x, y, z, false) { Index = (h, k, l), d = 1 / glen, Extinction = extinction, Text = $"{h} {k} {l}" };
            VectorOfG[i * 2 + 1] = new Vector3D(-x, -y, -z, false) { Index = (-h, -k, -l), d = 1 / glen, Extinction = extinction, Text = $"{-h} {-k} {-l}" };
        });

        if (VectorOfG != null && VectorOfG.Length > 0 && wavesource != WaveSource.None)//強度計算する場合 250msくらい
        {
            Parallel.ForEach(VectorOfG, _g =>
            {
                _g.F = _g.Extinction.Length == 0 ? GetStructureFactor(wavesource, Atoms, _g.Index, _g.Length2 / 4.0) : 0;
                _g.RawIntensity = _g.F.MagnitudeSquared();// _g.F.Magnitude2();
            });

            var maxIntensity = VectorOfG.Max(v => v.RawIntensity);
            Parallel.ForEach(VectorOfG, _g => _g.RelativeIntensity = _g.RawIntensity / maxIntensity);
        }
        VectorOfG_P = VectorOfG.AsParallel();
    }

    #endregion

    #region 菊池線逆格子ベクトルの計算

    //d_limit以上の範囲で菊池線逆格子ベクトルを計算
    public void SetVectorOfG_KikuchiLine(double d_limit, WaveSource wavesource)
    {
        if (A_Star == null) SetAxis();
        int hMax = (int)(A / d_limit);
        int kMax = (int)(B / d_limit);
        int lMax = (int)(C / d_limit);
        VectorOfG_KikuchiLine = [];
        for (int h = 0; h <= hMax; h++)
            for (int k = h == 0 ? 0 : -kMax; k <= kMax; k++)
                for (int l = (h == 0 && k == 0) ? 1 : -lMax; l <= lMax; l++)
                {
                    var temp = (h * A_Star + k * B_Star + l * C_Star).ToVector3D();
                    if ((temp.d = temp.Length) < 1 / d_limit)
                    {
                        temp.d = 1 / temp.Length;
                        temp.Text = $"{h} {k} {l}";
                        temp.Index = (h, k, l);
                        temp.Extinction = Symmetry.CheckExtinctionRule(h, k, l);

                        VectorOfG_KikuchiLine.Add(temp);
                    }
                }
        if (VectorOfG_KikuchiLine.Count == 0)
            return;


        Parallel.ForEach(VectorOfG_KikuchiLine, _g =>
        {
            _g.F = _g.Extinction.Length == 0 ? GetStructureFactor(wavesource, Atoms, _g.Index, _g.Length2 / 4.0) : 0;
            _g.RawIntensity = _g.F.MagnitudeSquared();// _g.F.Magnitude2();
        });

        var maxIntensity = VectorOfG_KikuchiLine.Max(v => v.RawIntensity);
        Parallel.ForEach(VectorOfG_KikuchiLine, _g => _g.RelativeIntensity = _g.RawIntensity / maxIntensity);

        VectorOfG_KikuchiLine.Sort((g1, g2) => g1.RelativeIntensity.CompareTo(g2.RelativeIntensity));
    }

    #endregion

    #region 回折強度の強いものを検索

    /// <summary>
    /// 現在の結晶構造で強度が上位最大8位までのものを検索し、返す
    /// </summary>
    /// <param name="waveLentgh">X線の波長を指定 単位はnm </param>
    /// <param name="count"> 計算する結晶面の数 </param>
    /// <returns></returns>
    public float[] GetDspacingList(double waveLentgh, int count = 1000)
    {
        SetPlanes(double.MaxValue, waveLentgh / 2, true, true, false, false, 0, 0, 0, count);

        SetPeakIntensity(WaveSource.Xray, WaveColor.Monochrome, waveLentgh, null);

        //強度の順にソート
        Plane.Sort((p1, p2) => -p1.Intensity.CompareTo(p2.Intensity));

        return [.. Plane.Take(Math.Min(8, Plane.Count)).Select(p => (float)p.d)];
    }
    #endregion

    #region 原子の追加/削除

    //引数の原子を加える
    public bool AddAtoms(Atoms Atoms)
    {
        return AddAtoms(Atoms, true);
    }

    public bool AddAtoms(Atoms atoms, bool RenewFormulaAndDensity)
    {
        if (Atoms.Length > 0)
        {
            Atoms[] temp = new Atoms[Atoms.Length + 1];
            Array.Copy(Atoms, temp, Atoms.Length);
            temp[Atoms.Length] = atoms;
            temp[Atoms.Length].ID = Atoms[^1].ID + 1;
            Atoms = temp;
            if (RenewFormulaAndDensity)
                GetFormulaAndDensity();
            return true;
        }
        else
        {
            Atoms = new Atoms[1];
            Atoms[0] = atoms;
            if (RenewFormulaAndDensity)
                GetFormulaAndDensity();
            return true;
        }
    }

    public bool AddAtoms(string label, int atomicNumber, int subXray, int subElectron, double[] isotope, double x, double y, double z, double occ, DiffuseScatteringFactor dsf)
    {
        return AddAtoms(label, atomicNumber, subXray, subElectron, isotope, x, y, z, occ, dsf, true);
    }

    //引数の原子を加える
    public bool AddAtoms(string label, int atomicNumber, int subXray, int subElectron, double[] isotope, double x, double y, double z, double occ, DiffuseScatteringFactor dsf, bool RenewFormulaAndDensity)
    {
        Atoms atoms = new(label, atomicNumber, subXray, subElectron, isotope, SymmetrySeriesNumber, new Vector3D(x, y, z), occ, dsf);
        return AddAtoms(atoms, RenewFormulaAndDensity);
    }

    //引数の原子を削除する
    public bool DeleteAtoms(Atoms atoms)
    {
        for (int i = 0; i < Atoms.Length; i++)
        {
            if (Atoms[i].ID == atoms.ID)
            {
                var temp = new Atoms[Atoms.Length - 1];
                Array.Copy(Atoms, 0, temp, 0, i);
                Array.Copy(Atoms, i + 1, temp, i, temp.Length - i);
                GetFormulaAndDensity();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// ワイコフ位置を保ったまま、原子位置を乱数的に変化させる
    /// </summary>
    /// <param name="seed"></param>
    public void RandomizeAllAtomicPositionKeepingWykoff(Random r)
    {
        for (int i = 0; i < Atoms.Length; i++)
            Atoms[i].RandomizeKeepingWykoff(r);
    }

    /// <summary>
    /// 原子の位置を再調整して、もっとも原点位置に近い原子をAtoms[i]=0に入れる
    /// </summary>
    public void ReCoordinateAtom()
    {
        for (int i = 0; i < Atoms.Length; i++)
        {
            double minR = double.PositiveInfinity;
            double r;
            int k = -1;
            for (int j = 0; j < Atoms[i].Atom.Length; j++)
                if ((r = Atoms[i].Atom[j].Length2) < minR)
                {
                    minR = r;
                    k = j;
                }
            if (k >= 0)
            {
                Atoms[i].X = Atoms[i].Atom[k].X;
                Atoms[i].Y = Atoms[i].Atom[k].Y;
                Atoms[i].Z = Atoms[i].Atom[k].Z;
            }
        }
    }


    #endregion

    #region 構造因子の計算

    [NonSerialized]
    [XmlIgnore]
    private static readonly Complex TwoPiI = 2 * Complex.ImaginaryOne * Math.PI;

    //(h,k,l)の構造散乱因子(熱散漫散乱込み)のF (複素数) を計算する (h, k, lが非整数の場合に対応させたテストコード)
    /// <summary>
    /// 構造因子を求める s2の単位はnm^-2
    /// </summary>
    /// <param name="wave"></param>
    /// <param name="atomsArray"></param>
    /// <param name="h"></param>
    /// <param name="k"></param>
    /// <param name="l"></param>
    /// <param name="s2">単位はnm^-2</param>
    /// <returns></returns>
    public static Complex GetStructureFactor(WaveSource wave, Atoms[] atomsArray, (double h, double k, double l) index, double s2)
    {
        #region
        (double h, double k, double l) = index;
        //s2 = (sin(theta)/ramda)^2 = 1 / 4 /d^2
        if (atomsArray.Length == 0)
            return new Complex(0, 0);
        Complex F = 0, f = 0;
        int atomicNum = -1, subNum = -1;

        foreach (var atoms in atomsArray)
        {
            if (wave == WaveSource.Electron)
            {
                if (atoms.AtomicNumber != atomicNum || atoms.SubNumberElectron != subNum)
                {
                    f = new Complex(atoms.GetAtomicScatteringFactorForElectron(s2), 0);
                    atomicNum = atoms.AtomicNumber;
                    subNum = atoms.SubNumberElectron;
                }
            }
            else if (wave == WaveSource.Xray)
            {
                if (atoms.AtomicNumber != atomicNum || atoms.SubNumberXray != subNum)
                {
                    f = new Complex(atoms.GetAtomicScatteringFactorForXray(s2), 0);
                    atomicNum = atoms.AtomicNumber;
                    subNum = atoms.SubNumberXray;
                }
            }
            else
                f = atoms.GetAtomicScatteringFactorForNeutron();


            if (atoms.Dsf.UseIso)
            {
                var T = Math.Exp(-atoms.Dsf.Biso * s2);
                if (double.IsNaN(T))
                    T = 1;
                foreach (var atom in atoms.Atom)
                    F += f * T * Complex.Exp(-TwoPiI * (h * atom.X + k * atom.Y + l * atom.Z));
            }
            else
            {
                foreach (var atom in atoms.Atom)
                {
                    var (H, K, L) = atom.Operation.ConvertPlaneIndex(h, k, l);
                    var T = Math.Exp(-(atoms.Dsf.B11 * H * H + atoms.Dsf.B22 * K * K + atoms.Dsf.B33 * L * L
                        + 2 * atoms.Dsf.B12 * H * K + 2 * atoms.Dsf.B23 * K * L + 2 * atoms.Dsf.B31 * L * H));
                    if (double.IsNaN(T))
                        T = 1;
                    F += f * T * Complex.Exp(-TwoPiI * (h * atom.X + k * atom.Y + l * atom.Z));
                }
            }
        }
        return F;// Complex(Real, Inverse);
        #endregion
    }


    //(h,k,l)の構造散乱因子(熱散漫散乱込み)のF (複素数) を計算する
    /// <summary>
    /// 構造因子を求める s2の単位はnm^-2
    /// </summary>
    /// <param name="wave"></param>
    /// <param name="atomsArray"></param>
    /// <param name="h"></param>
    /// <param name="k"></param>
    /// <param name="l"></param>
    /// <param name="s2">単位はnm^-2</param>
    /// <returns></returns>
    private static Complex GetStructureFactor(WaveSource wave, Atoms[] atomsArray, (int h, int k, int l) index, double s2)
    {
        #region
        (int h, int k, int l) = index;
        //s2 = (sin(theta)/ramda)^2 = 1 / 4 /d^2
        if (atomsArray.Length == 0)
            return new Complex(0, 0);
        Complex F = 0, f = 0;
        int atomicNum = -1, subNum = -1;

        foreach (var atoms in atomsArray)
        {
            if (wave == WaveSource.Electron)
            {
                if (atoms.AtomicNumber != atomicNum || atoms.SubNumberElectron != subNum)
                {
                    f = new Complex(atoms.GetAtomicScatteringFactorForElectron(s2), 0);
                    atomicNum = atoms.AtomicNumber;
                    subNum = atoms.SubNumberElectron;
                }
            }
            else if (wave == WaveSource.Xray)
            {
                if (atoms.AtomicNumber != atomicNum || atoms.SubNumberXray != subNum)
                {
                    f = new Complex(atoms.GetAtomicScatteringFactorForXray(s2), 0);
                    atomicNum = atoms.AtomicNumber;
                    subNum = atoms.SubNumberXray;
                }
            }
            else
                f = atoms.GetAtomicScatteringFactorForNeutron();


            if (atoms.Dsf.UseIso)
            {
                var T = Math.Exp(-atoms.Dsf.Biso * s2);
                if (double.IsNaN(T))
                    T = 1;
                foreach (var atom in atoms.Atom)
                    F += f * T * Complex.Exp(-TwoPiI * (h * atom.X + k * atom.Y + l * atom.Z));
            }
            else
            {
                foreach (var atom in atoms.Atom)
                {
                    var (H, K, L) = atom.Operation.ConvertPlaneIndex(h, k, l);
                    var T = Math.Exp(-(atoms.Dsf.B11 * H * H + atoms.Dsf.B22 * K * K + atoms.Dsf.B33 * L * L
                        + 2 * atoms.Dsf.B12 * H * K + 2 * atoms.Dsf.B23 * K * L + 2 * atoms.Dsf.B31 * L * H));
                    if (double.IsNaN(T))
                        T = 1;
                    F += f * T * Complex.Exp(-TwoPiI * (h * atom.X + k * atom.Y + l * atom.Z));
                }
            }
        }
        return F;// Complex(Real, Inverse);
        #endregion
    }

    /// <summary>
    /// 粉末回折実験における(h,k,l)の回折強度と位置を計算する
    /// </summary>
    /// <param name="ramda">波長</param>
    public void SetPeakIntensity(WaveSource waveSource, WaveColor waveColor, double ramda, Profile whiteProfile)
    {
        #region
        if (Atoms == null || Atoms.Length == 0 || Plane == null || Plane.Count == 0) return;

        for (int i = 0; i < Plane.Count; i++)
        {
            var sinTheta = ramda / 2 / Plane[i].d;
            var twoTheta = Plane[i].XCalc = 2 * Math.Asin(sinTheta);
            var cosTwoTheta = 1 - 2 * sinTheta * sinTheta;
            var sinTwoTheta = Math.Sin(twoTheta);

            var s = Plane[i].strHKL.Split('+', true);
            Plane[i].F2 = new double[s.Length];
            Plane[i].F = new Complex[s.Length];
            Plane[i].eachIntensity = new double[s.Length];
            var d2 = Plane[i].d * Plane[i].d;

            for (int j = 0; j < s.Length; j++)
            {
                if (s.Length == 1)
                    Plane[i].F[j] = GetStructureFactor(waveSource, Atoms, (Plane[i].h, Plane[i].k, Plane[i].l), 1 / d2 / 4.0);
                else
                {
                    var hkl = s[j].Split(' ', true);
                    int h = Convert.ToInt32(hkl[0]), k = Convert.ToInt32(hkl[1]), l = Convert.ToInt32(hkl[2]);
                    Plane[i].F[j] = GetStructureFactor(waveSource, Atoms, (h, k, l), 1 / d2 / 4.0);
                }

                Plane[i].F2[j] = Plane[i].F[j].MagnitudeSquared();

                if (waveSource == WaveSource.Xray)
                {
                    if (waveColor == WaveColor.Monochrome)
                        Plane[i].eachIntensity[j] = Plane[i].F2[j] * Plane[i].Multi[j] / CellVolumeSquare * (1 + cosTwoTheta * cosTwoTheta) / sinTwoTheta / sinTheta;
                    else if (waveColor == WaveColor.FlatWhite)
                        Plane[i].eachIntensity[j] = Plane[i].F2[j] * Plane[i].Multi[j] / CellVolumeSquare * d2;
                }
                else if (waveSource == WaveSource.Electron)
                {
                    Plane[i].eachIntensity[j] = Plane[i].F2[j] * Plane[i].Multi[j] / CellVolumeSquare / sinTwoTheta / sinTheta;
                }
                else if (waveSource == WaveSource.Neutron)
                {
                    if (waveColor == WaveColor.Monochrome)
                        Plane[i].eachIntensity[j] = Plane[i].F2[j] * Plane[i].Multi[j] / CellVolumeSquare / sinTwoTheta / sinTheta;
                    else
                        Plane[i].eachIntensity[j] = Plane[i].F2[j] * Plane[i].Multi[j] / CellVolumeSquare * d2 * d2;
                }
            }

            Plane[i].RawIntensity = 0;
            for (int j = 0; j < s.Length; j++)
                Plane[i].RawIntensity += Plane[i].eachIntensity[j];
        }

        var max = Plane.Max(p => p.RawIntensity);
        for (int i = 0; i < Plane.Count; i++)
        {
            //Plane[i].XCalc = 2 * Math.Asin(ramda / 2 / Plane[i].d) / Math.PI * 180;
            Plane[i].Intensity = Plane[i].RawIntensity / max;
            for (int j = 0; j < Plane[i].eachIntensity.Length; j++)
                Plane[i].eachIntensity[j] /= max;
        }
        #endregion
    }

    #endregion

    #region 対称性・原子位置・組成などの再計算
    /// <summary>
    /// 対称性、原子の位置、組成などを再計算する
    /// </summary>
    internal void Reset()
    {
        Symmetry = SymmetryStatic.Symmetries[SymmetrySeriesNumber];
        SetAxis();
        for (int i = 0; i < Atoms.Length; i++)
        {
            if (Symmetry.CrystalSystemStr == "hexagonal" || Symmetry.CrystalSystemStr == "trigonal")
            {
                double value66667;
                double value33333;
                if (Miscellaneous.IsDecimalPointComma)
                {
                    _ = double.TryParse(",66667", out value66667);
                    _ = double.TryParse(",33333", out value33333);
                }
                else
                {
                    _ = double.TryParse(".66667", out value66667);
                    _ = double.TryParse(".33333", out value33333);
                }

                if (Atoms[i].X == value66667) Atoms[i].X = 2.0 / 3.0;
                if (Atoms[i].X == value33333) Atoms[i].X = 1.0 / 3.0;
                if (Atoms[i].Y == value66667) Atoms[i].Y = 2.0 / 3.0;
                if (Atoms[i].Y == value33333) Atoms[i].Y = 1.0 / 3.0;
            }
            Atoms[i].ResetSymmetry(SymmetrySeriesNumber);
        }
        GetFormulaAndDensity();
    }


    /// <summary>
    /// 現在の原子種と格子定数から、組成と密度を計算します。
    /// </summary>
    public void GetFormulaAndDensity()
    {
        #region
        if (Atoms == null || Atoms.Length == 0) return;

        var dic = new Dictionary<string, double>();

        for (int i = 0; i < Atoms.Length; i++)
        {
            var key = Atoms[i].ElementName.Split(' ', true)[1];
            var num = Atoms[i].Multiplicity * Atoms[i].Occ;
            if (!dic.TryAdd(key, num))
                dic[key] += num;
        }

        ElementName = [.. dic.Keys];
        ElementNum = [.. dic.Values];

        if (ElementNum.Sum() == 0)
            return;


        //整数で割れるところまでわる
        if (ElementName != null)
        {
            var Num = ElementNum.ToArray();

            //まずNumの最小値をさがす
            int n = int.MaxValue;
            foreach (double var in Num)
                if (n > var)
                    n = (int)var;

            //次に2から順番にわっていく
            for (int i = 2; i <= n; i++)
            {
                bool IsDivisor = true;
                foreach (double var in Num)
                    if (var % i != 0) IsDivisor = false;
                if (IsDivisor)
                {
                    for (int j = 0; j < Num.Length; j++) Num[j] = Num[j] / i;
                    i--;
                }
            }

            //単位格子あたりの組成式の数Zは
            ChemicalFormulaZ = (int)(ElementNum[0] / Num[0]);

            //組成式 Fe21.333333333 O32  Fe2O3   *32/3 のように表示させる
            bool is3times = true;
            int denom = 0;
            for (int i = 0; i < Num.Length; i++)
                if (Math.Abs((Num[i] / 3.0)) % 1.0 > 0.000000001)
                    is3times = false;

            if (is3times == true)
            {
                denom = (int)(Num[0] * 3 + 0.1);
                for (int i = 0; i < Num.Length; i++)
                    Num[i] = (int)(Num[i] * 3 + 0.1);
                //まずNumの最小値をさがす
                int m = int.MaxValue;
                foreach (double var in Num)
                    if (m > var)
                        m = (int)var;

                //次に2から順番にわっていく
                for (int i = 2; i <= m; i++)
                {
                    bool IsDivisor = true;
                    foreach (double var in Num)
                        if (var % i != 0)
                            IsDivisor = false;
                    if (IsDivisor)
                    {
                        for (int j = 0; j < Num.Length; j++)
                            Num[j] = Num[j] / i;
                        i--;
                    }
                }
                denom /= (int)Num[0];
            }

            ChemicalFormulaSum = "";
            for (int i = 0; i < Num.Length; i++)
            {
                if (Num[i] == 1)
                    ChemicalFormulaSum += ElementName[i] + " ";
                else
                    ChemicalFormulaSum += $"{ElementName[i]}{Num[i].Round(12)} ";
            }
            ChemicalFormulaSum = ChemicalFormulaSum[0..^1];

            if (is3times && denom != 3)
                ChemicalFormulaSum += $"  *{denom}/3";

            double TotalWeightPerUnitCell = 0;
            for (int i = 0; i < ElementName.Length; i++)
            {
                TotalWeightPerUnitCell += AtomStatic.AtomicWeight(ElementName[i]) * ElementNum[i];
            }
            Density = TotalWeightPerUnitCell / Math.Sqrt(CellVolumeSquare) / 6.0221367 / 100;
            WeightPerFormula = TotalWeightPerUnitCell / ChemicalFormulaZ;
        }
        return;
        #endregion
    }

    #endregion

    #region 多結晶体の関係
    public void SetCrystallites() => Crystallites = new Crystallite(this);

    public void SetCrystallites(double[] density) => Crystallites = new Crystallite(this, density);

    #endregion

    #region 初期格子定数の保存、読み込み
    public void SaveInitialCellConstants()
    {
        InitialA = A;
        InitialB = B;
        InitialC = C;
        InitialAlpha = Alpha;
        InitialBeta = Beta;
        InitialGamma = Gamma;
        InitialA_err = A_err;
        InitialB_err = B_err;
        InitialC_err = C_err;
        InitialAlpha_err = Alpha_err;
        InitialBeta_err = Beta_err;
        InitialGamma_err = Gamma_err;
    }

    public void RevertInitialCellConstants()
    {
        if (InitialA != 0)
        {
            A = InitialA;
            B = InitialB;
            C = InitialC;
            Alpha = InitialAlpha;
            Beta = InitialBeta;
            Gamma = InitialGamma;
            A_err = InitialA_err;
            B_err = InitialB_err;
            C_err = InitialC_err;
            Alpha_err = InitialAlpha_err;
            Beta_err = InitialBeta_err;
            Gamma_err = InitialGamma_err;
            SetAxis();
        }
    }
    #endregion

    #region エクスポート

    public void ExportCIF(string filename)
    {
        using var sw = new StreamWriter(filename, false);
        sw.Write(ConvertCrystalData.ConvertToCIF(this));
    }


    #endregion

    #region 指定した原子(target)の近辺にある原子を探索し、相対座標、距離、ラベルを返す. (絶対座標でないことに注意)
    /// <summary>
    /// 指定した原子(target)の近辺にある原子を探索し、相対座標(絶対座標でないことに注意)、距離、ラベルを返す. 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="maxLength"> nm 単位</param>
    /// <returns></returns>
    public List<(double X, double Y, double Z, double Distance, string Label)> Search(Atoms target, double maxLength)
    {
        Vector3DBase pos = MatrixReal * target.Atom[0];
        var maxLen2 = maxLength * maxLength;
        var result = new List<(double X, double Y, double Z, double Distance, string Label)>();
        //まず、隣り合った単位格子の原子位置をすべて探索してresultリストに全部入れる 
        for (int max = 0; max < 8; max++)
        {
            bool flag = false;
            Parallel.For(-max, max + 1, xShift =>
            {
                for (int yShift = -max; yShift <= max; yShift++)
                    for (int zShift = -max; zShift <= max; zShift++)
                    {
                        if (Math.Abs(xShift) == max || Math.Abs(yShift) == max || Math.Abs(zShift) == max)
                        {
                            foreach (var atm in Atoms)
                                foreach (var v in atm.Atom)
                                {
                                    var diffPos = MatrixReal * (v + new Vector3DBase(xShift, yShift, zShift)) - pos;
                                    if (maxLen2 > (diffPos).Length2)
                                    {
                                        lock (lockObj)
                                        {
                                            result.Add((diffPos.X, diffPos.Y, diffPos.Z, diffPos.Length, atm.Label));
                                            flag = true;//一個でも見つけられたら続行　
                                        }
                                    }
                                }
                        }
                    }
            });
            if (flag == false && max > 2)
                break;
        }

        result.Sort((a1, a2) => a1.Distance.CompareTo(a2.Distance));
        return result;
    }
    #endregion
}
