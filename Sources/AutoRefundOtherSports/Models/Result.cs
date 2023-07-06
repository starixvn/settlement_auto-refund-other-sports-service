namespace AutoRefundOtherSports.Models;

public class Result
{
    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public bool IsSuccessful => string.IsNullOrWhiteSpace(ErrMsg) || (ErrCode == 0);
}