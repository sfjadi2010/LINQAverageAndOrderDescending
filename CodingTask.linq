async Task Main()
{
	var csvUrl = "https://data.cityofnewyork.us/api/views/r75y-8qe7/rows.csv?accessType=DOWNLOAD";
	
	IWebApi webApi = new WebApi(csvUrl);	
	ICsvFileReader csvFileReader = new CsvFileReader(webApi);
	IScoreList scoreList = new ScoreList(csvFileReader);	
	var processData = new ProcessData(scoreList);
	
	var data = await processData.GetProcessData();
	data.Dump();
}

// Define other methods and classes here

public interface ICsvFileReader {
	Task<String> GetCsv();
}

public interface IScoreList {
	Task<List<Score>> GetScores();
}

public interface IWebApi {
	HttpWebResponse GetDataByUrl();
}

public class Score {
	public string Grade { get; set; }
	public int Year { get; set; }
	public string Category { get; set; }
	public int NumberLevelTwo { get; set; }
}

public class ProcessedData {
	public string Category {get; set;}
	public int Year { get; set; }
	public double Average { get; set; }
}

public class WebApi : IWebApi {

	private readonly string _url;
	
	public WebApi(string url)
	{
		this._url = url;
	}
	
	public HttpWebResponse GetDataByUrl()
	{
		var request = (HttpWebRequest)WebRequest.Create(_url);
		var response = (HttpWebResponse)request.GetResponse();
		
		return response;
	}
}

public class CsvFileReader : ICsvFileReader {
	private readonly IWebApi _webApi;
	
	public CsvFileReader(IWebApi webApi)
	{
		this._webApi = webApi;
	}
	
	public async Task<String> GetCsv() {
		var results = default(string);
		
		var response = _webApi.GetDataByUrl();
		
		using(var streamReader = new StreamReader(response.GetResponseStream())) {
			await streamReader.ReadLineAsync();
			results = await streamReader.ReadToEndAsync();	
		}
		
		return results;
	}
	
}

public class ScoreList: IScoreList {
	
	private readonly ICsvFileReader _csvFileReader;
	
	public ScoreList(ICsvFileReader csvFileReader)
	{
		this._csvFileReader = csvFileReader;
	}

	public async Task<List<Score>> GetScores()
	{
		var results = await _csvFileReader.GetCsv();
		var data = results.Trim().Split('\n');

		IEnumerable<Score> scores = from scoreData in data
									let scoreLine = scoreData.Split(',')
									where (scoreLine[0] != "All Grades")
									select new Score()
									{
										Grade = scoreLine[0],
										Year = Convert.ToInt32(scoreLine[1]),
										Category = scoreLine[2],
										NumberLevelTwo = Convert.ToInt32(scoreLine[7]),
									};

		return scores.ToList();
	}
}

public class ProcessData {
	private readonly IScoreList _scoreList;
	
	public ProcessData(IScoreList scoreList)
	{
		this._scoreList = scoreList;
	}
	
	public async Task<List<ProcessedData>> GetProcessData()
	{
		var scoreList = await _scoreList.GetScores();
		var processedData = scoreList.GroupBy(t => new { t.Category, t.Year })
		.Select(grp => new ProcessedData(){
			Category = grp.Key.Category,
			Year = grp.Key.Year,
			Average = Math.Round(grp.Average(t => t.NumberLevelTwo), 2)
		}).OrderByDescending(O => O.Category).ThenByDescending(O => O.Year);
		
		return processedData.ToList();
	}
}
