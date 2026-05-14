'use strict';

const { Router } = require('express');
const Joi = require('joi');

const { authLimiter } = require('../middleware/rateLimit');
const { validateBody } = require('../middleware/validate');
const { loginGuest, loginGoogle } = require('../auth/service');

const router = Router();

const guestSchema = Joi.object({
  deviceId: Joi.string().min(8).max(128).required(),
  name: Joi.string().min(2).max(20).optional(),
});

const googleSchema = Joi.object({
  idToken: Joi.string().min(20).required(),
});

router.post('/guest', authLimiter, validateBody(guestSchema), async (req, res, next) => {
  try {
    const result = await loginGuest(req.body);
    res.json(result);
  } catch (e) { next(e); }
});

router.post('/google', authLimiter, validateBody(googleSchema), async (req, res, next) => {
  try {
    const result = await loginGoogle(req.body);
    res.json(result);
  } catch (e) { next(e); }
});

module.exports = router;
