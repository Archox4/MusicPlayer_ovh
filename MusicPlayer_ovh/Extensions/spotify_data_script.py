from spotify_scraper import SpotifyClient
import json
import sys

def get_info(url: str):
    client = SpotifyClient(log_level="ERROR")

    try:
        playlist = client.get_playlist_info(url)
        song_list = []
        for track in playlist["tracks"]:
            artists = ", ".join([artist["name"] for artist in track["artists"]])
            song_list.append({track["name"]:artists})
        print(json.dumps(song_list, ensure_ascii=False))
    except Exception as err:
        error = []
        if "Failed to extract" in str(err):
            error.append({"Error":"Make sure url is correct and playlist is public!"})
        else:
            error.append({"Error":"Unexpected error"})
        print(json.dumps(error))
    finally:
        client.close()

if __name__ == "__main__":
    get_info(sys.argv[1])