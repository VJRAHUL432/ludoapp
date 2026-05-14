'use strict';

const logger = require('../config/logger');

function notFoundHandler(req, res, _next) {
  res.status(404).json({ error: 'not_found' });
}

// eslint-disable-next-line no-unused-vars
function errorHandler(err, req, res, _next) {
  const status = err.status || err.statusCode || 500;
  if (status >= 500) {
    logger.error({ err, path: req.path }, 'request failed');
  } else {
    logger.warn({ msg: err.message, path: req.path, status }, 'request error');
  }
  res.status(status).json({
    error: err.code || (status >= 500 ? 'internal_error' : 'request_error'),
    message: status >= 500 ? 'Something went wrong' : err.message,
  });
}

module.exports = { notFoundHandler, errorHandler };
