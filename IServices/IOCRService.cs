namespace IDClassificationNew1.IServices
{
    public interface IOCRService
    {
        Task<string> ReadAllTextAsync(IFormFile imageFile);
        Task<string> ReadCropped_MarginXY_TextAsync(/*IFormFile imageFile*/ string grayPath, float marginXRatio, float marginYRatio);
        Task<string> ReadCropped_Custom4_TextAsync(/*IFormFile imageFile*/ string grayPath, float xL, float xR, float yU, float yD);
        Task<string> ReadGrayTextAsync(IFormFile imageFile);
    }
}
