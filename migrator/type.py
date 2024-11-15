

class FType:
	def __init__(self, data: dict):
		self.data = data

		self.seasonal = ("Seasonal" in self.data) and self.data["Seasonal"]

		self.source_image = self.data["Source Image"]

	def migrate(self):
		pass