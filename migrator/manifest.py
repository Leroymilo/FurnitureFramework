from pathlib import Path
from json import load, JSONDecodeError

from decoder import JSONCDecoder

class Manifest:
	def __init__(self, folder: Path):
		path = folder / "manifest.json"
		
		try:
			with open(path) as f:
				self.data = load(f, cls=JSONCDecoder)
		except OSError:
			print(f"Missing {path}!")
			self.data = None
			return
		except JSONDecodeError as e:
			print(f"JSON at {path} could not be loaded: {e}")
			self.data = None
			return