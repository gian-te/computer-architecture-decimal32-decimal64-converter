using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Decimal3264
{
    public partial class Form1 : Form
    {
        #region CONSTANTS
        private const int Decimal32MinExponentValue = -95;
        private const int Decimal32MaxExponentValue = 96;
        private const int Decimal32ExponentBias = 101;
        private const decimal Decimal32SmallestSubnormalValue = 0.000001M;
        private const int Decimal32MinValue = -9999999;
        private const int Decimal32MaxValue = 9999999;


        private const int Decimal64MinExponentValue = -383;
        private const int Decimal64MaxExponentValue = 384;
        private const int Decimal64ExponentBias = 398;
        private const decimal Decimal64SmallestSubnormalValue = 0.000000000000001M;
        private const long Decimal64MinValue = -9999999999999999;
        private const long Decimal64MaxValue = 9999999999999999;
        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Converts a decimal number to decimal-32 floating point format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ConvertToDecimal32FloatingPoint();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        /// <summary>
        /// Converts a decimal number to decimal-64 floating point format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                ConvertToDecimal64FloatingPoint();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// [GT] converts user input to a decimal64 floating point type.
        /// </summary>
        private void ConvertToDecimal64FloatingPoint()
        {
            bool isSubnormal = false;
            var txtDecimal = textBox6.Text.Trim();
            var txtExponent = textBox5.Text.Trim();
          
            txtDecimal = txtDecimal != "0" ? txtDecimal.TrimStart('0') : txtDecimal;
            long int64 = 0;
            int exponent = 0;

            // try if it can be parsed to a normalized integer with an integer exponent.
            if (!Int32.TryParse(txtExponent, out exponent))
            {
                MessageBox.Show("The exponent is not a valid 32-bit signed integer.");
            }
            if (txtDecimal.Contains("."))
            {
                // there is a decimal point, we will adjust the exponent as needed ("normalizing" the value)
                // ex. 7.25 x 10^5 == 725 x 10^3
                decimal value = 0.0M;
                if (Decimal.TryParse(txtDecimal, out value))
                {
                    // If it is a real number, we will try to normalize.
                    /* Examples:
                    7.25 to 725
                    .35 to 35
                    10.02 to 1002
                    .400 to 4
                    5.400 to 54
                    5000.40 to 50004
                    */

                    if (value < Decimal64SmallestSubnormalValue && value > 0) // if between subnormal value and zero, error
                    {
                        throw new Exception(string.Format("The input {0} is below the Decimal32 subnormal value of {1}", value, Decimal32SmallestSubnormalValue));
                    }
                    else if (value >= Decimal64SmallestSubnormalValue && value > 0 && value < 1) // if between 0 and 1, and within the acceptable subnormal range,
                    {
                        isSubnormal = true;
                    }

                    int decimalPlacesJumped = NormalizeInput(txtDecimal, out txtDecimal); 
                    if (decimalPlacesJumped != 0)
                    {
                        MessageBox.Show(string.Format("The input {0} can be normalized. See the input field for the new normalized value and exponent after dismissing this dialog box. ", textBox6.Text));
                        txtDecimal = txtDecimal.TrimEnd('.');
                        exponent -= decimalPlacesJumped;
                        textBox6.Text = txtDecimal.TrimStart('0');
                        textBox5.Text = exponent.ToString();
                    }
                }
                else
                {
                    throw new Exception(string.Format("The input {0} contains a decimal point but was unable to be parsed as a decimal number. Check the value and try again.", txtDecimal));
                }
            }

            if (!Int64.TryParse(txtDecimal, out int64))
            {
                throw new Exception("The input is not valid.");
            }
            if (IsZero(int64))
            {
                throw new Exception("Input must not be zero.");
            }
            if (!isSubnormal)
            {
                CheckIfBetweenValues(exponent, Decimal64MinExponentValue, Decimal64MaxExponentValue);

            }
            else
            {
                if (exponent < -398)
                {
                    throw new Exception("Exponents of subnormal values must be greater than or equal to -398");
                }
            }
            CheckIfBetweenValues(int64, Decimal64MinValue, Decimal64MaxValue);
            exponent += Decimal64ExponentBias;

            var exponentBinary = Convert.ToString(exponent, 2);
            exponentBinary = AddSignBitToBinary(exponentBinary, exponent, 10);
            var signBit = GetSignBit(int64);
            var absoluteValue = Math.Abs(int64);

            var sixteenDigitValue = absoluteValue.ToString("D16");
            var mostSignificantDigit = Convert.ToInt32(sixteenDigitValue[0].ToString());
            var combinationFld = DetermineCombinationField(exponentBinary, mostSignificantDigit);
            var exponentContinuationFld = exponentBinary.Substring(2, 8);

            // generate mantissa continuation bit for remaining digits
            var remainingFifteen = sixteenDigitValue.Substring(1, 15);
            var firstThreefRemainingFifteen = remainingFifteen.Substring(0, 3);
            var secondThreeOfRemainingFifteen = remainingFifteen.Substring(3, 3);
            var thirdThreeOfRemainingFifteen = remainingFifteen.Substring(6, 3);
            var fourthThreeOfRemainingFifteen = remainingFifteen.Substring(9, 3);
            var fifthThreeOfRemainingFifteen = remainingFifteen.Substring(12, 3);


            var firstTenOfMantissaContinuation = GenerateDPD(firstThreefRemainingFifteen);
            var secondTenOfMantissaContinuation = GenerateDPD(secondThreeOfRemainingFifteen);
            var thirdTenOfMantissaContinuation = GenerateDPD(thirdThreeOfRemainingFifteen);
            var fourthTenOfMantissaContinuation = GenerateDPD(fourthThreeOfRemainingFifteen);
            var fifthTenOfMantissaContinuation = GenerateDPD(fifthThreeOfRemainingFifteen);


            textBox3.Text = signBit.ToString() + " " + combinationFld + " " + exponentContinuationFld + " " + firstTenOfMantissaContinuation + " " + secondTenOfMantissaContinuation + " " + thirdTenOfMantissaContinuation + " " + fourthTenOfMantissaContinuation + " " + fifthTenOfMantissaContinuation;
            var decimal64Binary = signBit.ToString() + combinationFld +  exponentContinuationFld + firstTenOfMantissaContinuation + secondTenOfMantissaContinuation + thirdTenOfMantissaContinuation + fourthTenOfMantissaContinuation + fifthTenOfMantissaContinuation;

            var tempDecimal = Convert.ToInt64(decimal64Binary, 2);
            var hexValue = Convert.ToString(tempDecimal, 16);
            textBox8.Text = hexValue.ToString(); ;
        }

        /// <summary>
        /// [GT] converts user input to a decimal32 floating point type.
        /// </summary>
        private void ConvertToDecimal32FloatingPoint()
        {
            bool isSubnormal = false;
            var txtDecimal = textBox1.Text.Trim();
            var txtExponent = textBox4.Text.Trim();
         
            txtDecimal = txtDecimal != "0" ? txtDecimal.TrimStart('0') : txtDecimal;
            int int32 = 0;
            int exponent = 0;

            // try if it can be parsed to a normalized integer with an integer exponent.
            if (!Int32.TryParse(txtExponent, out exponent))
            {
                MessageBox.Show("The exponent is not a valid 32-bit signed integer.");
            }
            if (txtDecimal.Contains("."))
            {
                // there is a decimal point, we will adjust the exponent as needed ("normalizing" the value)
                // ex. 7.25 x 10^5 == 725 x 10^3
                decimal value = 0.0M;
                if (Decimal.TryParse(txtDecimal, out value))
                {
                    // If it is a real number, we will try to normalize.
                    /* Examples:
                    7.25 to 725
                    .35 to 35
                    10.02 to 1002
                    .400 to 4
                    5.400 to 54
                    5000.40 to 50004
                    */
                    if (value < Decimal32SmallestSubnormalValue && value > 0) // if between subnormal value and zero, error
                    {
                        throw new Exception(string.Format("The input {0} is below the Decimal32 subnormal value of {1}", value, Decimal32SmallestSubnormalValue));
                    }
                    else if (value >= Decimal32SmallestSubnormalValue && value > 0 && value < 1) // if between 0 and 1, and within the acceptable subnormal range,
                    {
                        isSubnormal = true;
                    }
                    int decimalPlacesJumped = NormalizeInput(txtDecimal, out txtDecimal); ;
                    if (decimalPlacesJumped != 0 )
                    {
                        MessageBox.Show(string.Format("The input {0} can be normalized. See the input field for the new normalized value and exponent after dismissing this dialog box. ", textBox1.Text));
                        txtDecimal = txtDecimal.TrimEnd('.');
                        exponent -= decimalPlacesJumped;
                        textBox1.Text = txtDecimal.TrimStart('0');
                        textBox4.Text = exponent.ToString();
                    }
                }
                else
                {
                    throw new Exception(string.Format("The input {0} contains a decimal point but was unable to be parsed as a decimal number. Check the value and try again.", txtDecimal));
                }
            }

            if (!Int32.TryParse(txtDecimal, out int32))
            {
                // the input is a normal integer, no need to adjust the exponent
                throw new Exception("The input is not valid.");
            }
            if (IsZero(int32))
            {
                throw new Exception("Input must not be zero.");
            }

            if (!isSubnormal)
            {
                CheckIfBetweenValues(exponent, Decimal32MinExponentValue, Decimal32MaxExponentValue);

            }
            else
            {
                if (exponent < -101)
                {
                    throw new Exception("Exponents of subnormal values must be greater than or equal to -101");
                }
            }
            CheckIfBetweenValues(int32, Decimal32MinValue, Decimal32MaxValue);
            exponent += Decimal32ExponentBias;

            var exponentBinary = Convert.ToString(exponent, 2);
            exponentBinary = AddSignBitToBinary(exponentBinary, exponent, 8);
            var signBit = GetSignBit(int32);
            var absoluteValue = Math.Abs(int32);
            var sevenDigitValue = absoluteValue.ToString("D7");
            var mostSignificantDigit = Convert.ToInt32(sevenDigitValue[0].ToString());
            var combinationFld = DetermineCombinationField(exponentBinary, mostSignificantDigit);
            var exponentContinuationFld = exponentBinary.Substring(2, 6);

            // generate mantissa continuation bit for remaining 6 digits
            var remainingSix = sevenDigitValue.Substring(1, 6);
            var firstHalfOfRemainingSix = remainingSix.Substring(0, 3);
            var secondHalfOfRemainingSix = remainingSix.Substring(3, 3);
            var firstHalfOfMantissaContinuation = GenerateDPD(firstHalfOfRemainingSix);
            var secondHalfOfMantissaContinuation = GenerateDPD(secondHalfOfRemainingSix);

            textBox2.Text = signBit.ToString() + " " + combinationFld + " " + exponentContinuationFld + " " + firstHalfOfMantissaContinuation + " " + secondHalfOfMantissaContinuation;
            var decimal32Binary = signBit.ToString() + combinationFld + exponentContinuationFld + firstHalfOfMantissaContinuation + secondHalfOfMantissaContinuation;

            var tempDecimal = Convert.ToInt32(decimal32Binary, 2);
            var hexValue = Convert.ToString(tempDecimal, 16);
            textBox7.Text = hexValue.ToString(); ;
        }

        private bool IsZero(Int64 number)
        {
            return number == 0;
        }

        /// <summary>
        /// [GT] generates a densely packed decimal encoded number
        /// </summary>
        /// <param name="threeDigitNumber"></param>
        /// <returns></returns>
        private string GenerateDPD(string threeDigitNumber)
        {
            string retVal = "";
            string packedBCD = GeneratePackedBCD(threeDigitNumber);
            // given the packed BCD, map to a table to get the DPD, then done.

            //notes: mapping of letter to index
            //a=0
            //b=1
            //c=2
            //d=3
            //e=4
            //f=5
            //g=6
            //h=7
            //i=8
            //j=9
            //k=10
            //m=11
            
            if (GetAEI(packedBCD) == "000")
            {
                // return bcd fgh 0 jk m
                retVal += GetBitInPosition(packedBCD, 1); //b
                retVal += GetBitInPosition(packedBCD, 2); //c
                retVal += GetBitInPosition(packedBCD, 3); //d

                retVal += GetBitInPosition(packedBCD, 5); //f
                retVal += GetBitInPosition(packedBCD, 6); //g
                retVal += GetBitInPosition(packedBCD, 7); //h

                retVal += "0"; // v-bit

                retVal += GetBitInPosition(packedBCD, 9); //j
                retVal += GetBitInPosition(packedBCD, 10); //k
                retVal += GetBitInPosition(packedBCD, 11); //m
            }
            else if (GetAEI(packedBCD) == "001")
            {
                // return bcd fgh 1 00 m
                retVal += GetBitInPosition(packedBCD, 1); //b
                retVal += GetBitInPosition(packedBCD, 2); //c
                retVal += GetBitInPosition(packedBCD, 3); //d

                retVal += GetBitInPosition(packedBCD, 5); //f
                retVal += GetBitInPosition(packedBCD, 6); //g
                retVal += GetBitInPosition(packedBCD, 7); //h

                retVal += "1"; // v-bit

                retVal += "0"; //0
                retVal += "0"; //0

                retVal += GetBitInPosition(packedBCD, 11); //m
            }
            else if (GetAEI(packedBCD) == "010")
            {
                // return bcd jkh 1 01 m
                retVal += GetBitInPosition(packedBCD, 1); //b
                retVal += GetBitInPosition(packedBCD, 2); //c
                retVal += GetBitInPosition(packedBCD, 3); //d

                retVal += GetBitInPosition(packedBCD, 9); //j
                retVal += GetBitInPosition(packedBCD, 10); //k
                retVal += GetBitInPosition(packedBCD, 7); //h

                retVal += "1"; // v-bit

                retVal += "0"; //0
                retVal += "1"; //1

                retVal += GetBitInPosition(packedBCD, 11); //m
            }
            else if (GetAEI(packedBCD) == "011")
            {
                // return bcd 10h 1 11 m
                retVal += GetBitInPosition(packedBCD, 1); //b
                retVal += GetBitInPosition(packedBCD, 2); //c
                retVal += GetBitInPosition(packedBCD, 3); //d

                retVal += "1"; //1
                retVal += "0"; //0
                retVal += GetBitInPosition(packedBCD, 7); //h

                retVal += "1"; // v-bit

                retVal += "1"; //1
                retVal += "1"; //1

                retVal += GetBitInPosition(packedBCD, 11); //m
            }
            else if (GetAEI(packedBCD) == "100")
            {
                // return jkd fgh 1 10 m
                retVal += GetBitInPosition(packedBCD, 9); //j
                retVal += GetBitInPosition(packedBCD, 10); //k
                retVal += GetBitInPosition(packedBCD, 3); //d

                retVal += GetBitInPosition(packedBCD, 5); //f
                retVal += GetBitInPosition(packedBCD, 6); //g
                retVal += GetBitInPosition(packedBCD, 7); //h

                retVal += "1"; // v-bit

                retVal += "1"; //1
                retVal += "0"; //0

                retVal += GetBitInPosition(packedBCD, 11); //m
            }
            else if (GetAEI(packedBCD) == "101")
            {
                // return fgd 01h 1 11 m
                retVal += GetBitInPosition(packedBCD, 5); //f
                retVal += GetBitInPosition(packedBCD, 6); //g
                retVal += GetBitInPosition(packedBCD, 3); //d

                retVal += "0"; //0
                retVal += "1"; //1
                retVal += GetBitInPosition(packedBCD, 7); //h

                retVal += "1"; // v-bit

                retVal += "1"; //1
                retVal += "1"; //1  

                retVal += GetBitInPosition(packedBCD, 11); //m
            }
            else if (GetAEI(packedBCD) == "110")
            {
                // return jkd 00h 1 11m
                retVal += GetBitInPosition(packedBCD, 9); //j
                retVal += GetBitInPosition(packedBCD, 10); //k
                retVal += GetBitInPosition(packedBCD, 3); //d

                retVal += "0"; //0
                retVal += "0"; //0
                retVal += GetBitInPosition(packedBCD, 7); //h

                retVal += "1"; // v-bit

                retVal += "1"; //1
                retVal += "1"; //1  

                retVal += GetBitInPosition(packedBCD, 11); //m
            }
            else if (GetAEI(packedBCD) == "111")
            {
                // return 00d 11h 1 11 m
                retVal += "0"; //0
                retVal += "0"; //0
                retVal += GetBitInPosition(packedBCD, 3); //d

                retVal += "1"; //1
                retVal += "1"; //1  
                retVal += GetBitInPosition(packedBCD, 7); //h

                retVal += "1"; // v-bit

                retVal += "1"; //1
                retVal += "1"; //1  

                retVal += GetBitInPosition(packedBCD, 11); //m
            }

            return retVal;
        }

        /// <summary>
        /// [GT] gets a bit from a string based on position
        /// </summary>
        /// <param name="packedBCD"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private string GetBitInPosition(string packedBCD, int index)
        {
            string retVal = "";
            retVal = packedBCD[index].ToString();
            return retVal;
        }

        /// <summary>
        /// [GT] returns the AEI of a packed BCD
        /// </summary>
        /// <param name="packedBCD"></param>
        /// <returns></returns>
        private string GetAEI(string packedBCD)
        {
            string retVal = "";
            retVal += packedBCD[0].ToString();
            retVal += packedBCD[4].ToString();
            retVal += packedBCD[8].ToString();
            return retVal;
        }

        /// <summary>
        /// [GT] converts a 3-digit number to packed BCD, returning 12 bits.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private string GeneratePackedBCD(string number)
        {
            string retVal = "";
            foreach (var character in number)
            {
                var digit = Convert.ToInt32(character.ToString());
                var digitInBinary = Convert.ToString(digit, 2);
                var prepended = AddSignBitToBinary(digitInBinary, digit, 4);
                retVal += prepended;
            }

            if (retVal.Length != 12)
            {
                throw new Exception(string.Format("The packed BCD that was generated is not 12 bits.\nInput: {0} \nOutput: {1}", number, retVal));
            }
            return retVal;
        }

        /// <summary>
        /// [GT] Normalizes user input
        /// </summary>
        /// <param name="txtDecimal"></param>
        /// <param name="txtNormalizedDecimal"></param>
        /// <returns></returns>
        private int NormalizeInput(string txtDecimal, out string txtNormalizedDecimal)
        {
            var idx = txtDecimal.IndexOf('.');
            txtDecimal = txtDecimal.TrimEnd('0');
            txtDecimal = txtDecimal.Remove(idx, 1);
            // trim trailing zeroes

            txtDecimal += ".";
            var newIdx = txtDecimal.IndexOf('.');

            txtNormalizedDecimal = txtDecimal;
            return newIdx - idx;
        }

        /// <summary>
        /// Prepends a 0 or 1 to the binary number depending on the sign, until a certain number of length.
        /// </summary>
        /// <param name="numberInBinary"></param>
        /// <param name="number"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private string AddSignBitToBinary(string numberInBinary, int number, int length)
        {
            if (numberInBinary.Length < length)
            {
                int loop = length - numberInBinary.Length;
                int prependVal = 0;
                if (IsNegative(number))
                {
                    prependVal = 1;
                }
                for (int i = 0; i < loop; i++)
                {
                    numberInBinary = prependVal + numberInBinary;
                }
            }

            return numberInBinary;
        }

        private bool IsNegative(int num)
        {
            if (num < 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// [GT] returns a 5-bit combination field.
        /// </summary>
        /// <param name="exponentBinary"></param>
        /// <param name="mostSignificantDigit"></param>
        /// <returns></returns>
        private string DetermineCombinationField(string exponentBinary, int mostSignificantDigit)
        {
            string retVal = "";

            if (mostSignificantDigit > 9)
            {
                throw new Exception(string.Format("MSD is > 9. Value: {0}", mostSignificantDigit));
            }
            var twoMostSignificantBits = exponentBinary.Substring(0, 2);

            var msdBinary = Convert.ToString(mostSignificantDigit, 2);
            msdBinary = AddSignBitToBinary(msdBinary, mostSignificantDigit, 4);
            if (mostSignificantDigit == 8 || mostSignificantDigit == 9)
            {
                retVal += "11";
                retVal += twoMostSignificantBits;
                retVal += msdBinary.Substring(msdBinary.Length - 1, 1);
            }
            else
            {
                retVal += twoMostSignificantBits;
                retVal += msdBinary.Substring(1, 3);
            }

            if (retVal.Length != 5)
            {
                throw new Exception(string.Format("Combination field is not equal to 5. Value: {0}", retVal));
            }

            return retVal;
        }

        /// <summary>
        /// [GT] Checks if the supplied exponent is between two numbers
        /// </summary>
        /// <param name="number"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        private void CheckIfBetweenValues(long number, long min, long max)
        {
            if (number > max || number < min)
            {
                throw new Exception(string.Format("The value {0} is smaller than the minimum value of {1} or greater than the maximum value of {2}", number, min, max));
            }
        }

        /// <summary>
        /// [GT] Returns 1 as the sign bit if the supplied argument is negative, otherwise it returns 0.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private int GetSignBit(long number)
        {
            var retVal = 0; // positive
            if (number < 0) // if negative
            {
                retVal = 1;
            }
            return retVal;
        }
    }
}
