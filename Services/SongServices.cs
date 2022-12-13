using tourmaline.Helpers;
using tourmaline.Models;

namespace tourmaline.Services;

public class SongServices
{
    private readonly Database _database;

    public SongServices(Database database)
    {
        _database = database;
    }

    public async Task<Song> GetSong(long id)
    {
        var result = (await _database.Call($"SELECT * FROM song WHERE id={id}")).First();
        return new Song
        {
            Id = result["id"],
            Name = result["name"],
            Uploader = result["uploader"],
            UploadTime = result["uploadTime"],
            Description = result["description"],
            Duration = result["duration"],
            Favorites = await GetFavorite(id),
            ListenTimes = result["listen_times"],
            Tags = await GetTags(id),
        };
    }

    public async Task AddSong(Song song)
    {
        await _database.Call($"INSERT INTO song (id, uploadTime, uploader, name, description, duration, listen_times) " +
        $"VALUES ({song.Id}, '{song.UploadTime:yyyy-MM-dd H:mm:ss}', '{song.Uploader}', '{song.Name.Normal()}', '{song.Description.Normal()}', {song.Duration}, {song.ListenTimes})");
    }
    
    private async Task<int> GetFavorite(long id)
    {
        var result = (await _database.Call($"SELECT COUNT(*) as count FROM favorites WHERE songid={id}")).First();
        return result["count"];
    }

    public async Task AddListenTime(long id)
    {
        var listenTimes = (await GetSong(id)).ListenTimes + 1;
        await _database.Call($"UPDATE song WHERE id={id} SET listen_times={listenTimes}");
    }

    private async Task<List<string>> GetTags(long id)
    {
        var result = await _database.Call($"SELECT tag FROM songtags WHERE id={id}");
        return result.Select(element => element["tag"]).Cast<string>().ToList();
    }

    private async Task SetTags(long id, List<string> tags)
    {
        await _database.Call($"DELETE FROM songtags WHERE id={id}");
        foreach (var tag in tags)
        {
            await _database.Call($"INSERT IGNORE INTO tags (tag) VALUES ('{tag.Normal()}')");
            await _database.Call($"INSERT IGNORE INTO songtags (id, tag) VALUES ({id}, '{tag.Normal()}')");
        }
    }

    public async Task DeleteSong(long id)
    {
        await _database.Call($"DELETE FROM song WHERE id={id}");
    }

    public async Task UpdateInfo(long id, string? name, string? description, List<string>? tags)
    {
        await _database.Call($"UPDATE song " +
                             $"WHERE id={id} " +
                             $"SET {(name != null ? $"name='{name}'" : "")}, {(description != null ? $"description='{description}'" : "")}");
        if (tags != null) await SetTags(id, tags);
    }

    public async Task<bool> IsSongExist(long id)
    {
        return (await _database.Call($"SELECT * FROM song WHERE id={id}")).Count != 0;
    }

    public async Task<List<Song>> GetUserUploads(string username)
    {
        var result = await _database.Call($"SELECT * FROM song WHERE uploader='{username}'");
        var songs = new List<Song>();
        foreach (var song in result)
        {
            songs.Add(new Song
            {
                Id = song["id"],
                Name = song["name"],
                Uploader = song["uploader"],
                UploadTime = song["uploadTime"],
                Description = song["description"],
                Duration = song["duration"],
                Favorites = await GetFavorite(song["id"]),
                ListenTimes = song["listen_times"],
                Tags = await GetTags(song["id"]),
            });
        }

        return songs;
    }
}