class _Registry:
    def __init__(self):
        self._objects = {}

    def register(self, obj=None):
        def _do(o):
            self._objects[o.__name__] = o
            return o
        if obj is None:
            return _do
        return _do(obj)

    def get(self, name):
        return self._objects[name]


ARCH_REGISTRY = _Registry()
