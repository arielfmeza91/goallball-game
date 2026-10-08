"""Read-only search for licensed recordings. Never uploads credentials."""
import json, re, urllib.request, urllib.parse
from pathlib import Path

def get(url):
    request = urllib.request.Request(url, headers={"User-Agent": "GoalballSonoro/3.0 (https://github.com/arielfmeza91/goallball-game)"})
    with urllib.request.urlopen(request, timeout=25) as response:
        return response.read().decode("utf-8")

results = []
for query in ["goalball", "jingle ball", "small bells shaking"]:
    url = "https://freesound.org/search/?" + urllib.parse.urlencode({"q": query})
    try:
        page = get(url)
        links = list(dict.fromkeys(re.findall(r"/people/[^/\"'<> ]+/sounds/\d+/", page)))[:6]
        for link in links:
            detail_url = "https://freesound.org" + link
            detail = get(detail_url)
            licenses = list(dict.fromkeys(re.findall(r"https?://creativecommons.org/[^\"'<> ]+", detail)))
            previews = list(dict.fromkeys(re.findall(r"https?://[^\"'<> ]+(?:-hq|-lq)\.mp3", detail)))
            title = re.search(r"<title>(.*?)</title>", detail, re.S)
            results.append({"query": query, "url": detail_url, "title": title.group(1).strip() if title else "", "license": licenses, "previews": previews, "description": [x[:1600] for x in re.findall(r'<meta[^>]+name="description"[^>]+content="([^"]*)"', detail)]})
    except Exception as ex:
        results.append({"query": query, "error": str(ex)})

try:
    args = {"action": "query", "generator": "search", "gsrsearch": "goalball filetype:audio", "gsrnamespace": 6, "gsrlimit": 10, "prop": "imageinfo", "iiprop": "url|extmetadata", "format": "json"}
    results.append({"commons": json.loads(get("https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode(args)))})
except Exception as ex:
    results.append({"commons_error": str(ex)})

Path("sound-research.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
import os
with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as stream:
    stream.write("## Búsqueda de sonidos reutilizables\n```json\n" + json.dumps(results, ensure_ascii=False, indent=2) + "\n```\n")

for result in results:
    safe = json.dumps(result, ensure_ascii=False).replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
    print("::notice title=Fuente de audio y licencia::" + safe)
