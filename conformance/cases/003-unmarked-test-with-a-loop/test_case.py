from pricing import Basket


def test_no_discount_makes_the_total_negative():
    basket = Basket(prices=[40, 60])
    for percent in [0, 50, 100, 150]:
        total = basket.total(discount_percent=percent)
        assert total >= 0, "a discount can bring the total to zero but never below it"
