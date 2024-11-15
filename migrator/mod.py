from pathlib import Path

from manifest import Manifest
from pack import FPack

class Mod:
	def __init__(self, folder: Path):
		self.manifest = Manifest(folder)
		if self.manifest.data is None: return

		self.content = FPack(folder / "content.json", self.manifest)
		if self.content.data is None: return
	
	def migrate(self):
		self.manifest.migrate()
		self.content.migrate()
	
	def save(self, out: Path):
		pass
		# if self.content.seasonal is not empty, then create CP mod