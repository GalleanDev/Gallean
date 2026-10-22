import logging

def get_root_logger(*args, **kwargs):
    return logging.getLogger("basicsr")
