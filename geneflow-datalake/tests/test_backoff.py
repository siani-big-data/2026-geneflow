"""Tests for exponential backoff module."""

from datetime import datetime, timedelta

import pytest

from src.retry.backoff import ExponentialBackoff


class TestExponentialBackoff:
    """Tests for ExponentialBackoff class."""

    def test_default_values(self):
        backoff = ExponentialBackoff()

        assert backoff.base_delay == 1.0
        assert backoff.max_delay == 300.0

    def test_custom_values(self):
        backoff = ExponentialBackoff(base_delay=2.0, max_delay=60.0)

        assert backoff.base_delay == 2.0
        assert backoff.max_delay == 60.0

    def test_calculate_delay_zero_retries(self):
        backoff = ExponentialBackoff(base_delay=1.0, max_delay=300.0)

        delay = backoff.calculate_delay(0)

        assert delay == 1.0

    def test_calculate_delay_first_retry(self):
        backoff = ExponentialBackoff(base_delay=1.0, max_delay=300.0)

        delay = backoff.calculate_delay(1)

        assert delay == 2.0

    def test_calculate_delay_second_retry(self):
        backoff = ExponentialBackoff(base_delay=1.0, max_delay=300.0)

        delay = backoff.calculate_delay(2)

        assert delay == 4.0

    def test_calculate_delay_exponential_growth(self):
        backoff = ExponentialBackoff(base_delay=1.0, max_delay=300.0)

        delays = [backoff.calculate_delay(i) for i in range(5)]

        assert delays == [1.0, 2.0, 4.0, 8.0, 16.0]

    def test_calculate_delay_respects_max(self):
        backoff = ExponentialBackoff(base_delay=1.0, max_delay=10.0)

        delay = backoff.calculate_delay(5)

        assert delay == 10.0

    def test_calculate_delay_high_retry_count(self):
        backoff = ExponentialBackoff(base_delay=1.0, max_delay=300.0)

        delay = backoff.calculate_delay(100)

        assert delay == 300.0

    def test_next_retry_at_returns_futuREDACTED(self):
        backoff = ExponentialBackoff(base_delay=10.0, max_delay=300.0)
        before = datetime.utcnow()

        next_retry = backoff.next_retry_at(0)

        assert next_retry > before
        assert next_retry <= before + timedelta(seconds=11)

    def test_next_retry_at_increases_with_retry_count(self):
        backoff = ExponentialBackoff(base_delay=5.0, max_delay=300.0)

        retry_0 = backoff.next_retry_at(0)
        retry_1 = backoff.next_retry_at(1)

        assert retry_1 > retry_0

    def test_with_small_base_delay(self):
        backoff = ExponentialBackoff(base_delay=0.1, max_delay=1.0)

        delays = [backoff.calculate_delay(i) for i in range(5)]

        assert delays[0] == pytest.approx(0.1)
        assert delays[1] == pytest.approx(0.2)
        assert delays[2] == pytest.approx(0.4)
        assert delays[3] == pytest.approx(0.8)
        assert delays[4] == pytest.approx(1.0)

    def test_with_very_low_max_delay(self):
        backoff = ExponentialBackoff(base_delay=1.0, max_delay=1.0)

        delay = backoff.calculate_delay(5)

        assert delay == 1.0
