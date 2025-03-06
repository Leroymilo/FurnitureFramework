from copy import deepcopy

class Slot:
	def __init__(self, data: dict, rot_name: str, base_height: int):
		if rot_name in data:
			data = data[rot_name]
		
		if "Area" not in data: raise KeyError
		
		self.data = deepcopy(data)
		self.data["Area"]["Y"] -= base_height


class Slots:

	def __init__(self, data: dict | list, rot_name: str, base_height: int):
		self.data = []
		if type(data) is dict and rot_name in data:
			data = data[rot_name]
		
		if type(data) is list:
			self.data = [Slot(val, rot_name, base_height) for val in data]
		
		elif type(data) is dict:
			try:
				self.data = [Slot(data, rot_name, base_height)]
			except KeyError:
				self.data = []

	def to_json(self):
		return [layer.data for layer in self.data]