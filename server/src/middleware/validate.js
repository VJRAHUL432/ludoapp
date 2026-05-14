'use strict';

/**
 * Returns an Express middleware that validates `req.body` against a Joi schema.
 * On failure: 400 { error: 'validation', details }.
 */
function validateBody(schema) {
  return (req, res, next) => {
    const { error, value } = schema.validate(req.body, {
      abortEarly: false,
      stripUnknown: true,
    });
    if (error) {
      return res.status(400).json({
        error: 'validation',
        details: error.details.map((d) => d.message),
      });
    }
    req.body = value;
    next();
  };
}

module.exports = { validateBody };
