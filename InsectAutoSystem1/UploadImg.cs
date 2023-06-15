using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class UpdateImage
{
    private HttpClient httpClient;

    public UpdateImage()
    {
        httpClient = new HttpClient();
    }

    public async Task UploadImages(string apiUrl, string imagePath, string uploadPath, int id)
    {
        using (var form = new MultipartFormDataContent())
        {
            // 이미지 파일 업로드
            var imageContent = new ByteArrayContent(File.ReadAllBytes(imagePath));
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            form.Add(imageContent, "image", Path.GetFileName(imagePath));

            // API URL 생성
            string apiUrlWithParams = apiUrl
                .Replace("{uploadPath}", uploadPath)
                .Replace("{id}", id.ToString());

            // API 호출
            var response = await httpClient.PostAsync(apiUrlWithParams, form);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("image_upload_resut: " + result);
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
            }
        }
    }
}
