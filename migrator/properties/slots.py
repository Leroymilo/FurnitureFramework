from copy import deepcopy

class Slot:
	def __init__(self, data: dict, rot_name: str, base_height: int):
		if rot_name in data:
			self = Slot(data[rot_name], rot_name, base_height)
			return
		
		self.data = deepcopy(data)
		self.data["Area"]["Y"] -= base_height


class Slots:

	def __init__(self, data: dict | list, rot_name: str, base_height: int):
		self.data = []
		if type(data) is list:
			self.data = [Slot(data[i], rot_name, base_height) for i in range(data)]
		
		elif type(data) is dict:
			if rot_name in data:
				self = Slots(data[rot_name], rot_name, base_height)
				return
			
			self.data = [Slot(data, rot_name, base_height)]

	def to_json(self):
		return [layer.data for layer in self.data]